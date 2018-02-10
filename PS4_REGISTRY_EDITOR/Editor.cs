using System;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using Ps4EditLib;
using Ps4EditLib.Extensions;
using Ps4EditLib.PsRegistry;
using Ps4EditLib.Reader;

namespace PS4_REGISTRY_EDITOR
{
    /// <summary>
    /// TODO!!!
    /// Too much responsibility, restruct needed
    /// </summary>
    public partial class Editor : Form
    {
        private byte[] _data;
        private IEntityReader _registry;

        public Editor()
        {
            InitializeComponent();
        }

        private void Editor_Load(object sender, EventArgs e)
        {
            typeLabel.Hide();
            dataTextBox.Hide();
            dataLabel.Hide();
            saveButton.Hide();
            applyButton.Hide();
        }

        /// <summary>
        /// TODO: Method too long, unify and outsource some code
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFileButton_Click(object sender, EventArgs e)
        {
            var fileDialog = new OpenFileDialog
            {
                Filter = @"|*.*||system.idx;system.dat;system.eap;system.rec",
                RestoreDirectory = true
            };

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                _data = File.ReadAllBytes(fileDialog.FileName);

                var file = RegFile.Open(_data);

                if (file == null)
                {
                    MessageBox.Show("Invalid registry file!\nOpen system.idx or system.dat or system.eap or system.rec !");
                    return;
                }

                if (file.Storage == "regcont_eap.db")
                {
                    _registry = new ObfuscatedContainerReader(_data, false);
                }
                else if (file.Storage == "regi.recover")
                {
                    _registry = new ObfuscatedContainerReader(_data, true);
                }
                else
                {
                    if (file.Storage == "regcont.db")
                    {
                        var idx = _data;

                        fileDialog = new OpenFileDialog
                        {
                            Filter = @"|system.dat||*.*",
                            RestoreDirectory = true
                        };

                        if (fileDialog.ShowDialog() == DialogResult.OK)
                        {
                            _data = File.ReadAllBytes(fileDialog.FileName);

                            file = RegFile.Open(_data);

                            if (file == null || file.Storage != "regdatahdd.db")
                            {
                                MessageBox.Show(@"Invalid system.dat !");
                                return;
                            }

                            _registry = new DataContainerReader(_data, idx);
                        }
                    }
                    else if (file.Storage == "regdatahdd.db")
                    {
                        fileDialog = new OpenFileDialog
                        {
                            Filter = @"|system.idx||*.*",
                            RestoreDirectory = true
                        };

                        if (fileDialog.ShowDialog() == DialogResult.OK)
                        {
                            var idx = File.ReadAllBytes(fileDialog.FileName);

                            file = Preferences.RegFiles.Find(x => x.Size == idx.Length);

                            if (file == null || file.Storage != "regcont.db")
                            {
                                MessageBox.Show(@"Invalid system.idx !");
                                return;
                            }

                            _registry = new DataContainerReader(_data, idx);
                        }
                    }
                }

                listView.Clear();

                var header = new ColumnHeader { Width = listView.Width };
                listView.Columns.Add(header);
                listView.View = View.Details;
                listView.HeaderStyle = ColumnHeaderStyle.None;

                _registry.Entries.ForEach(x => listView.Items.Add(x.Category));
            }
        }

        private void listView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView.SelectedIndices.Count == 0)
                return;

            var selected = listView.SelectedIndices[0];

            var entry = _registry.Entries[selected];

            typeLabel.Show();
            dataTextBox.Show();
            dataLabel.Show();
            saveButton.Show();
            applyButton.Show();

            if (_registry.ObfuscatedContainer)
            {
                switch (entry.Type)
                {
                    case EntryType.Integer:
                        Crypto.XorData(_data, 0x20 + entry.I * 0x10, 0x10);
                        break;

                    case EntryType.String:
                    case EntryType.Binary:
                        Crypto.XorData(_data, entry.Offset - 4, entry.Size + 4);
                        break;
                }
            }

            if (entry.Type == EntryType.Integer)
            {
                dataTextBox.Text = BitConverter.ToUInt32(_data, entry.Offset).ToString();
            }
            else if (entry.Type == EntryType.String)
            {
                dataTextBox.Text = _data.Skip(entry.Offset).Take(entry.Size).ToHexString();
            }
            else if (entry.Type == EntryType.Binary)
            {
                dataTextBox.Text = _data.Skip(entry.Offset).Take(entry.Size).ToHexString();
            }

            typeLabel.Text = entry.Type.ToString();

            dataLabel.Text = _data
                                .Skip(entry.Offset)
                                .Take(entry.Size)
                                .ToArray()
                                .HexDump();

            if (_registry.ObfuscatedContainer)
            {
                switch (entry.Type)
                {
                    case EntryType.Integer:
                        Crypto.XorData(_data, 0x20 + entry.I * 0x10, 0x10);
                        break;

                    case EntryType.String:
                    case EntryType.Binary:
                        Crypto.XorData(_data, entry.Offset - 4, entry.Size + 4);
                        break;

                    default:
                        break;
                }
            }

            applyButton.Enabled = false;
        }

        /// <summary>
        /// TODO: Method too long, unify and outsource some code
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void applyButton_Click(object sender, EventArgs e)
        {
            if (listView.SelectedIndices.Count == 0 && _data != null)
                return;

            var selected = listView.SelectedIndices[0];

            var entry = _registry.Entries[selected];

            dataTextBox.Text = dataTextBox.Text.Replace(" ", string.Empty);

            if (entry.Type == EntryType.Integer)
            {
                if (_registry.ObfuscatedContainer)
                {
                    Crypto.XorData(_data, 0x20 + entry.I * 0x10, 0x10);
                }

                var value = Convert.ToUInt32(dataTextBox.Text);
                _data.Store32(entry.Offset, value);

                if (_registry.ObfuscatedContainer)
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    var newEntry = _data.Skip(0x20 + entry.I * 0x10).Take(0x10).ToArray();
                    newEntry.Store16(0xA, 0);
                    var entryHash = Crypto.CalcHash(newEntry, newEntry.Length, 2).Swap16();

                    _data.Store16(0x20 + entry.I * 0x10 + 0xA, entryHash);

                    Crypto.XorData(_data, 0x20 + entry.I * 0x10, 0x10);
                }

                applyButton.Enabled = false;
            }
            else if (entry.Type == EntryType.String || entry.Type == EntryType.Binary)
            {
                if (dataTextBox.Text.Length % 2 == 0 && dataTextBox.Text.Length / 2 == entry.Size)
                {
                    var patched = dataTextBox.Text.ToByteArray();

                    if (_registry.ObfuscatedContainer)
                    {
                        Crypto.XorData(_data, entry.Offset - 4, entry.Size + 4);
                    }

                    for (var i = 0; i < patched.Length; i++)
                    {
                        _data[entry.Offset + i] = patched[i];
                    }

                    if (_registry.ObfuscatedContainer)
                    {
                        var bin = _data.Skip(entry.Offset).Take(entry.Size).ToArray();

                        var binHash2 = Crypto.CalcHash(bin, bin.Length, 4).Swap32();
                        _data.Store32(entry.Offset - 4, binHash2);

                        Crypto.XorData(_data, entry.Offset - 4, entry.Size + 4);
                    }

                    applyButton.Enabled = false;
                }
                else
                {
                    MessageBox.Show(@"Wrong data size!");
                }
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (_data == null)
                return;

            var file = RegFile.Open(_data);

            if (file == null)
            {
                MessageBox.Show(@"Error saving file!");
                return;
            }

            var fileDialog = new SaveFileDialog
            {
                Filter = @"All files (*.*)|*.*",
                FileName = file.File.Split('/')[3],
                RestoreDirectory = true
            };

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                using (var fs = File.Open(fileDialog.FileName, FileMode.Create))
                {
                    fs.Write(_data, 0, _data.Length);
                }
            }
        }

        private void dataTextBox_TextChanged(object sender, EventArgs e)
        {
            applyButton.Enabled = true;
        }
    }
}