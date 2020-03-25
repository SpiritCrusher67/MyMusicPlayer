using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Media;
using NAudio.Wave;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using TagLib;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Win32;

namespace Player
{
    class MainModelView : ViewModelBase, INotifyPropertyChanged
    {
        private AudioFile _currentFile;

        private WaveOutEvent waveOut;
        private int oldVolume;
        private bool _isPlaying;
        private int _volume;
        private bool _isMuted;
        private VolumeState _volumeState;

        public int Volume { get => _volume; set { _volume = value; RaisePropertyChanged(); } }

        public VolumeState VolumeState { get => _volumeState; set { _volumeState = value; RaisePropertyChanged(); } }

        public bool IsPlaying { get => _isPlaying; set { _isPlaying = value; RaisePropertyChanged(); } }

        public bool IsMuted { get => _isMuted; set { _isMuted = value; RaisePropertyChanged(); } }

        public AudioFile CurrentFile
        {
            get => _currentFile;
            set
            {
                _currentFile = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<AudioFile> Files { get; set; }

        public MainModelView()
        {
            Files = new ObservableCollection<AudioFile>();
            waveOut = new WaveOutEvent();
        }

        public override void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.RaisePropertyChanged(propertyName);

            switch (propertyName)
            {
                case "CurrentFile":
                    if (CurrentFile != default)
                    {
                        waveOut.Stop();
                        waveOut.Init(new AudioFileReader(CurrentFile.Patch));
                        waveOut.Play();
                        IsPlaying = true;
                    }
                    break;
                case "Volume":
                    waveOut.Volume = Volume / 100f;
                    VolumeState = (Volume == 0) ? VolumeState.Muted : (Volume <= 40) ? VolumeState.Low : (Volume <= 70) ? VolumeState.Medium : VolumeState.High;
                    break;
                case "IsMuted":
                    if (IsMuted)
                    {
                        oldVolume = Volume;
                        Volume = 0;
                    }
                    else Volume = oldVolume;
                    break;
                default:
                    break;
            }

        }

        public RelayCommand AddFilesCommand
        {
            get => new RelayCommand(() =>
            {
                OpenFileDialog fileDialog = new OpenFileDialog();
                fileDialog.Multiselect = true;

                if (fileDialog.ShowDialog() == true)
                    foreach (var item in fileDialog.FileNames) Files.Add(new AudioFile(item));
            });
        }

        public RelayCommand PlayStopCommand
        {
            get => new RelayCommand(() =>
            {
                if (CurrentFile != default)
                {
                    switch (waveOut.PlaybackState)
                    {
                        case PlaybackState.Stopped:
                            waveOut.Init(new AudioFileReader(CurrentFile.Patch));
                            waveOut.Play();
                            IsPlaying = true;
                            break;
                        case PlaybackState.Playing:
                            waveOut.Pause();
                            IsPlaying = false;
                            break;
                        case PlaybackState.Paused:
                            waveOut.Play();
                            IsPlaying = true;
                            break;
                        default:
                            break;
                    }
                }

            });
        }

        public RelayCommand NextFileCommand
        {
            get => new RelayCommand(() =>
            {
                var index = Files.IndexOf(CurrentFile);
                if (index < Files.Count - 1)
                {
                    CurrentFile = Files[index + 1];
                }
                else
                {
                    CurrentFile = Files.FirstOrDefault();
                }
            });
        }
        public RelayCommand PreviousFileCommand
        {
            get => new RelayCommand(() =>
            {
                var index = Files.IndexOf(CurrentFile);
                if (index > 0)
                {
                    CurrentFile = Files[index - 1];
                }
                else
                {
                    CurrentFile = Files.LastOrDefault();
                }
            });
        }

        public RelayCommand MixFilesCommand
        {
            get => new RelayCommand(() =>
            {
                var list = Files.ToList();
                var i = 0;
                while (i < Files.Count)
                {
                    Files.Move(0, new Random().Next(0, Files.Count - 1));
                    i++;
                }


            });
        }

        public RelayCommand MuteCommand
        {

            get => new RelayCommand(() =>
            {
                oldVolume = Volume;

            });
        }
    }

    public enum VolumeState
    {
        Muted,
        Low,
        Medium,
        High
    }

    class AudioFile
    {
        public string Patch { get; set; }
        public uint Track { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public uint Year { get; set; }
        public TimeSpan Duration { get; set; }
        public byte[] Picture { get; set; }

        public AudioFile(string patch)
        {
            var tag = TagLib.File.Create(patch).Tag;
            
            Patch = patch;
            Track = tag.Track;
            Title = tag.Title;
            Artist = tag.AlbumArtists.FirstOrDefault();
            Album = tag.Album;
            Year = tag.Year;
            Duration = new AudioFileReader(patch).TotalTime;
            Picture = tag.Pictures.FirstOrDefault()?.Data.Data;
        }

    }
}
