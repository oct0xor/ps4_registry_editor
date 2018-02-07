using System;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using Ps4EditLib;
using Ps4EditLib.Extensions;
using Ps4EditLib.PsRegistry;
using Ps4EditLib.Reader;

namespace PS4_REGISTRY_EDITOR
{
    public partial class Editor : Form
    {
        private byte[] _data;
        private EntityReader _registry;

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
                    _registry = new EntityReader(_data, false);
                }
                else if (file.Storage == "regi.recover")
                {
                    _registry = new EntityReader(_data, true);
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

                            file = PsRegistry.RegFiles.Find(x => x.Size == _data.Length);

                            if (file == null && BitConverter.ToUInt32(_data, 4) == 0x2A2A2A2A)
                            {
                                file = PsRegistry.RegFiles.Find(x => x.Storage == "regdatahdd.db");
                            }

                            if (file == null || file.Storage != "regdatahdd.db")
                            {
                                MessageBox.Show(@"Invalid system.dat !");
                                return;
                            }

                            _registry = new EntityReader(_data, idx);
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

                            file = PsRegistry.RegFiles.Find(x => x.Size == idx.Length);

                            if (file == null || file.Storage != "regcont.db")
                            {
                                MessageBox.Show(@"Invalid system.idx !");
                                return;
                            }

                            _registry = new EntityReader(_data, idx);
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
                if (entry.Type == PsRegistry.Integer)
                {
                    Crypto.XorData(_data, 0x20 + entry.I * 0x10, 0x10);
                }
                else if (entry.Type == PsRegistry.String || entry.Type == PsRegistry.Binary)
                {
                    Crypto.XorData(_data, entry.Offset - 4, entry.Size + 4);
                }
            }

            if (entry.Type == PsRegistry.Integer)
            {
                dataTextBox.Text = BitConverter.ToUInt32(_data, entry.Offset).ToString();
                typeLabel.Text = "INTEGER";
            }
            else if (entry.Type == PsRegistry.String)
            {
                dataTextBox.Text = ByteUtilities.ByteArrayToString(_data.Skip(entry.Offset).Take(entry.Size));
                typeLabel.Text = "STRING";
            }
            else if (entry.Type == PsRegistry.Binary)
            {
                dataTextBox.Text = ByteUtilities.ByteArrayToString(_data.Skip(entry.Offset).Take(entry.Size));
                typeLabel.Text = "BINARY";
            }

            dataLabel.Text = ByteUtilities.HexDump(_data.Skip(entry.Offset).Take(entry.Size).ToArray());

            if (_registry.ObfuscatedContainer)
            {
                if (entry.Type == PsRegistry.Integer)
                {
                    Crypto.XorData(_data, 0x20 + entry.I * 0x10, 0x10);
                }
                else if (entry.Type == PsRegistry.String || entry.Type == PsRegistry.Binary)
                {
                    Crypto.XorData(_data, entry.Offset - 4, entry.Size + 4);
                }
            }

            applyButton.Enabled = false;
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            if (listView.SelectedIndices.Count == 0 && _data != null)
                return;

            var selected = listView.SelectedIndices[0];

            var entry = _registry.Entries[selected];

            dataTextBox.Text = dataTextBox.Text.Replace(" ", string.Empty);

            if (entry.Type == PsRegistry.Integer)
            {
                if (_registry.ObfuscatedContainer)
                {
                    Crypto.XorData(_data, 0x20 + entry.I * 0x10, 0x10);
                }

                var value = Convert.ToUInt32(dataTextBox.Text);
                _data.Store32(entry.Offset, value);

                if (_registry.ObfuscatedContainer)
                {
                    var newEntry = _data.Skip(0x20 + entry.I * 0x10).Take(0x10).ToArray();
                    newEntry.Store16(0xA, 0);
                    var entryHash = Crypto.CalcHash(newEntry, newEntry.Length, 2).Swap16();

                    _data.Store16(0x20 + entry.I * 0x10 + 0xA, entryHash);

                    Crypto.XorData(_data, 0x20 + entry.I * 0x10, 0x10);
                }

                applyButton.Enabled = false;
            }
            else if (entry.Type == PsRegistry.String || entry.Type == PsRegistry.Binary)
            {
                if (dataTextBox.Text.Length % 2 == 0 && dataTextBox.Text.Length / 2 == entry.Size)
                {
                    var patched = ByteUtilities.StringToByteArray(dataTextBox.Text);

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

            var file = PsRegistry.RegFiles.Find(x => x.Size == _data.Length);

            if (file == null && BitConverter.ToUInt32(_data, 4) == 0x2A2A2A2A)
            {
                file = PsRegistry.RegFiles.Find(x => x.Storage == "regdatahdd.db");
            }

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