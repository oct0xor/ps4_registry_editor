using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace PS4_REGISTRY_EDITOR
{
    public partial class Editor : Form
    {
        byte[] data;
        Reader registry;

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
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.Filter = "|*.*||system.idx;system.dat;system.eap;system.rec";
            fileDialog.RestoreDirectory = true;

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                data = File.ReadAllBytes(fileDialog.FileName);

                RegFile file = Registry.regFiles.Find(x => x.size == data.Length);

                if (file == null && BitConverter.ToUInt32(data, 4) == 0x2A2A2A2A)
                {
                    file = Registry.regFiles.Find(x => x.storage == "regdatahdd.db");
                }

                if (file == null)
                {
                    MessageBox.Show("Invalid registry file!\nOpen system.idx or system.dat or system.eap or system.rec !");
                    return;
                }

                if (file.storage == "regcont_eap.db")
                {
                    registry = new Reader();
                    registry.ObfuscatedContainerReader(data, false);
                }
                else if (file.storage == "regi.recover")
                {
                    registry = new Reader();
                    registry.ObfuscatedContainerReader(data, true);
                }
                else if (file.storage == "regcont.db")
                {
                    byte[] idx = data;

                    fileDialog = new OpenFileDialog();

                    fileDialog.Filter = "|system.dat||*.*";
                    fileDialog.RestoreDirectory = true;

                    if (fileDialog.ShowDialog() == DialogResult.OK)
                    {
                        data = File.ReadAllBytes(fileDialog.FileName);

                        file = Registry.regFiles.Find(x => x.size == data.Length);

                        if (file == null && BitConverter.ToUInt32(data, 4) == 0x2A2A2A2A)
                        {
                            file = Registry.regFiles.Find(x => x.storage == "regdatahdd.db");
                        }

                        if (file == null || file.storage != "regdatahdd.db")
                        {
                            MessageBox.Show("Invalid system.dat !");
                            return;
                        }

                        registry = new Reader();
                        registry.DataContainerReader(data, idx);
                    }
                }
                else if (file.storage == "regdatahdd.db")
                {
                    fileDialog = new OpenFileDialog();

                    fileDialog.Filter = "|system.idx||*.*";
                    fileDialog.RestoreDirectory = true;

                    if (fileDialog.ShowDialog() == DialogResult.OK)
                    {
                        byte[] idx = File.ReadAllBytes(fileDialog.FileName);

                        file = Registry.regFiles.Find(x => x.size == idx.Length);

                        if (file == null || file.storage != "regcont.db")
                        {
                            MessageBox.Show("Invalid system.idx !");
                            return;
                        }

                        registry = new Reader();
                        registry.DataContainerReader(data, idx);
                    }
                }

                listView.Clear();

                ColumnHeader header = new ColumnHeader();
                header.Width = listView.Width;
                listView.Columns.Add(header);
                listView.View = View.Details;
                listView.HeaderStyle = ColumnHeaderStyle.None;

                foreach (var entry in registry.entries)
                {
                    listView.Items.Add(entry.category);
                }
            }
        }

        private void listView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView.SelectedIndices.Count == 0)
                return;

            var selected = listView.SelectedIndices[0];

            Entry entry = registry.entries[selected];

            typeLabel.Show();
            dataTextBox.Show();
            dataLabel.Show();
            saveButton.Show();
            applyButton.Show();

            if (registry.obfuscatedContainer)
            {
                if (entry.type == Registry.INTEGER)
                {
                    Crypto.XorData(data, 0x20 + entry.i * 0x10, 0x10);
                }
                else if (entry.type == Registry.STRING || entry.type == Registry.BINARY)
                {
                    Crypto.XorData(data, entry.offset - 4, entry.size + 4);
                }
            }

            if (entry.type == Registry.INTEGER)
            {
                dataTextBox.Text = BitConverter.ToUInt32(data, entry.offset).ToString();
                typeLabel.Text = "INTEGER";
            }
            else if (entry.type == Registry.STRING)
            {
                dataTextBox.Text = Utils.ByteArrayToString(data.Skip(entry.offset).Take(entry.size).ToArray());
                typeLabel.Text = "STRING";
            }
            else if (entry.type == Registry.BINARY)
            {
                dataTextBox.Text = Utils.ByteArrayToString(data.Skip(entry.offset).Take(entry.size).ToArray());
                typeLabel.Text = "BINARY";
            }

            dataLabel.Text = Utils.HexDump(data.Skip(entry.offset).Take(entry.size).ToArray());

            if (registry.obfuscatedContainer)
            {
                if (entry.type == Registry.INTEGER)
                {
                    Crypto.XorData(data, 0x20 + entry.i * 0x10, 0x10);
                }
                else if (entry.type == Registry.STRING || entry.type == Registry.BINARY)
                {
                    Crypto.XorData(data, entry.offset - 4, entry.size + 4);
                }
            }

            applyButton.Enabled = false;
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            if (listView.SelectedIndices.Count == 0 && data != null)
                return;

            var selected = listView.SelectedIndices[0];

            Entry entry = registry.entries[selected];

            dataTextBox.Text = dataTextBox.Text.Replace(" ", string.Empty);

            if (entry.type == Registry.INTEGER)
            {
                if (registry.obfuscatedContainer)
                {
                    Crypto.XorData(data, 0x20 + entry.i * 0x10, 0x10);
                }

                uint value = Convert.ToUInt32(dataTextBox.Text);
                Utils.Store32(data, entry.offset, value);

                if (registry.obfuscatedContainer)
                {
                    byte[] newEntry = data.Skip(0x20 + entry.i * 0x10).Take(0x10).ToArray();
                    Utils.Store16(newEntry, 0xA, 0);
                    ushort entryHash = Utils.Swap16((ushort)Crypto.CalcHash(newEntry, newEntry.Length, 2));
                    Utils.Store16(data, 0x20 + entry.i * 0x10 + 0xA, entryHash);

                    Crypto.XorData(data, 0x20 + entry.i * 0x10, 0x10);
                }

                applyButton.Enabled = false;
            }
            else if (entry.type == Registry.STRING || entry.type == Registry.BINARY)
            {
                if (dataTextBox.Text.Length % 2 == 0 && dataTextBox.Text.Length / 2 == entry.size)
                {
                    byte[] patched = Utils.StringToByteArray(dataTextBox.Text);

                    if (registry.obfuscatedContainer)
                    {
                        Crypto.XorData(data, entry.offset - 4, entry.size + 4);
                    }

                    for (int i = 0; i < patched.Length; i++)
                    {
                        data[entry.offset + i] = patched[i];
                    }

                    if (registry.obfuscatedContainer)
                    {
                        byte[] bin = data.Skip(entry.offset).Take(entry.size).ToArray();
                        uint binHash2 = Utils.Swap32((uint)Crypto.CalcHash(bin, bin.Length, 4));
                        Utils.Store32(data, entry.offset - 4, binHash2);

                        Crypto.XorData(data, entry.offset - 4, entry.size + 4);
                    }

                    applyButton.Enabled = false;
                }
                else
                {
                    MessageBox.Show("Wrong data size!");
                }
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (data == null)
                return;

            RegFile file = Registry.regFiles.Find(x => x.size == data.Length);

            if (file == null && BitConverter.ToUInt32(data, 4) == 0x2A2A2A2A)
            {
                file = Registry.regFiles.Find(x => x.storage == "regdatahdd.db");
            }

            SaveFileDialog fileDialog = new SaveFileDialog();

            fileDialog.Filter = "All files (*.*)|*.*";

            fileDialog.FileName = file.file.Split('/')[3];

            fileDialog.RestoreDirectory = true;

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                using (BinaryWriter writer = new BinaryWriter(File.Open(fileDialog.FileName, FileMode.Create)))
                {
                    writer.Write(data);
                }
            }
        }

        private void dataTextBox_TextChanged(object sender, EventArgs e)
        {
            applyButton.Enabled = true;
        }
    }
}
