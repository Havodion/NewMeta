﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using File_Renamer.Properties;
using TagLib;
using File = TagLib.File;

namespace File_Renamer
{
    // TODO: Add menu bar
    /* Include:
     * Exit 
     * About
     */

    public partial class MainWindow : Form
    {
        private int _counter;
        private DirectoryInfo _directory;
        private string _filePath = string.Empty;
        private FileInfo[] _infos;
        private List<string> _namesList = new List<string>();
        private string _newTitle = "", _ext = "";
        private string _path;
        private File _tagFile;
        private int _totalFiles;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            // Added event to NumbicUpDown
            NumericUpDown.TextChanged += NumericUpDown_TextChanged;

            string location = Settings.Default["Location"].ToString();
            if (!string.IsNullOrEmpty(location))
            {
                MoveToDir(location);
            }
        }

        private void Save_Click(object sender, EventArgs e)
        {
            try
            {
                _newTitle = NewName.Text;
                if (NewName.Text.Equals(""))
                {
                    throw new Exception("File can not be saved without a name.");
                }
                string artist = Artist.Text;
                // TODO: add code to save the album as well.
                // string album = Album.Text;
                uint track = uint.Parse(TrackNum.Text);
                // Remove the trailing extension
                if (_newTitle.IndexOf('.') > 1)
                {
                    _tagFile.Tag.Title = _newTitle.Substring(0, _newTitle.IndexOf('.'));
                }
                else
                {
                    //Title
                    if (_tagFile.Tag.Title == null || !_tagFile.Tag.Title.Equals(_newTitle))
                    {
                        _tagFile.Tag.Title = _newTitle;
                    }
                }
                //Artist
                if (!_tagFile.Tag.FirstPerformer.Equals(artist))
                {
                    _tagFile.Tag.AlbumArtists[0] = artist;
                }
                //Track
                if (_tagFile.Tag.Track != track && track != 0)
                {
                    _tagFile.Tag.Track = track;
                }

                _tagFile.Save();
                _newTitle += _ext;
                System.IO.File.Move(_path, _directory + _newTitle);
                _namesList[_counter] = _directory + _newTitle;
                _counter++;
                GetNewFile();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void GetNewFile()
        {
            try
            {
                if (_totalFiles != 0)
                {
                    FilesLeft.ForeColor = Color.Black;
                    if (_totalFiles == _counter)
                    {
                        MessageBox.Show(@"All the files have been renamed.", @"Done", MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        _counter = 0;
                    }

                    string fileName =
                        _namesList[_counter].Substring(
                            _namesList[_counter].LastIndexOf("\\", StringComparison.Ordinal) + 1);
                    _directory =
                        new DirectoryInfo(_namesList[_counter].Substring(0,
                            _namesList[_counter].LastIndexOf("\\", StringComparison.Ordinal) + 1));
                    _path = _directory + fileName;
                    lblCurrentFolder.Text = _directory.FullName;
                    string currentName = fileName;
                    _ext = fileName.Substring(fileName.IndexOf('.'));
                    _tagFile = File.Create(_path);
                    string artist = _tagFile.Tag.FirstPerformer;
                    string album = _tagFile.Tag.Album;
                    int track = (int) _tagFile.Tag.Track;
                    if (artist == null) artist = "No Artist";
                    if (album == null) album = "No Album";
                    Artist.Text = artist;
                    Album.Text = album;
                    TrackNum.Text = track + "";
                    string title = _tagFile.Tag.Title;
                    string songName = !string.IsNullOrEmpty(title) ? title : currentName;

                    if (chkRUnderScore.Checked && songName.Contains('_'))
                    {
                        songName = songName.Replace('_', ' ');
                    }
                    if (chkUpperCase.Checked)
                    {
                        songName = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(songName);
                    }
                    NewName.Text = songName;

                    CurrentName.Text = currentName;
                    NumericUpDown.Text = "" + _counter;

                    FilesLeft.Text = (_totalFiles - _counter).ToString();
                }
                else
                {
                    FilesLeft.Text = @"No Files In Current Directory";
                    FilesLeft.ForeColor = Color.Red;
                }
            }
            catch (CorruptFileException)
            {
                //TODO: add functionality  for the user to rename files even if the meta data is corrupted.
                MessageBox.Show(@"The files meta data is corrupted,\nSkipping to new file.", @"Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _counter++;
                GetNewFile();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            System.IO.File.Delete(_path);
            _namesList.RemoveAt(_counter);
            _counter++;
            GetNewFile();
        }

        private void Back_Click(object sender, EventArgs e)
        {
            _newTitle = NewName.Text;
            _counter--;
            if (_counter < 0) _counter = 0;
            if (_counter > _namesList.Count()) _counter = _namesList.Count();
            GetNewFile();
        }

        private void NumericUpDown_TextChanged(object sender, EventArgs e)
        {
            int result;
            if (int.TryParse(NumericUpDown.Text, out result))
            {
                _counter = result;
                GetNewFile();
            }
        }

        private void btnSelectPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog {SelectedPath = Settings.Default.Location};
            DialogResult result = fbd.ShowDialog();

            if (result == DialogResult.OK)
            {
                _filePath = fbd.SelectedPath;
                Settings.Default["Location"] = _filePath;
                Settings.Default.Save();
                MoveToDir(_filePath);
            }
        }

        private void TrackNum_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
            {
                e.Handled = true;
            }
        }

        private void MoveToDir(string location)
        {
            // Valid Extensions that the program can handle
            List<string> validExtentions = new List<string> {".mp3", ".m4a", ".mp4"};
            _namesList = new List<string>();
            _counter = 0;
            _directory = new DirectoryInfo(location);
            _infos = _directory.GetFiles("*", SearchOption.AllDirectories);
            foreach (FileInfo item in _infos)
            {
                string ext = item.Name.Substring(item.Name.LastIndexOf('.'));
                if (validExtentions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                {
                    _namesList.Add(item.DirectoryName + "\\" + item.Name);
                }
            }
            _totalFiles = _namesList.Count();

            lblCurrentPath.Text = location;
            if (PanelMain.Visible == false)
            {
                PanelMain.Visible = true;
            }

            GetNewFile();
        }
    }
}