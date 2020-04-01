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
using System.Runtime.Remoting;
using System.Timers;
using System.Windows;

namespace Player
{
    class MainModelView : ViewModelBase, INotifyPropertyChanged
    {
        public MainModelView()
        {
            Files = new ObservableCollection<AudioFile>();
            waveOut = new WaveOutEvent();

            timer = new Timer();
            timer.Interval = 300;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private AudioFile _currentFile;

        private WaveOutEvent waveOut;
        private int oldVolume;
        private int _volume;
        private bool _isMuted;
        private VolumeState _volumeState;
        private TimeSpan _currentTime;
        private Timer timer;
        private long _currentPosition;
        private AudioFileReader _fileReader;

        public AudioFileReader FileReader { get => _fileReader; set { _fileReader = value; RaisePropertyChanged(); } }

        //Выбранный файл
        public AudioFile CurrentFile
        {
            get => _currentFile;
            set
            {
                if (_currentFile != value)
                {
                    _currentFile = value;

                    if (CurrentFile != default)
                    {
                        waveOut.Stop();
                        FileReader = new AudioFileReader(CurrentFile.Patch);
                        waveOut.Init(FileReader);
                        waveOut.Play();
                        RaisePropertyChanged("IsPlaying");
                        RaisePropertyChanged("IsStopped");
                        if (FileReader != null)
                        {
                            CurrentTime = FileReader.CurrentTime;
                        }
                     
                    }
                    RaisePropertyChanged();

                }
            }
        }

        public long CurrentPosition
        {
            get => _currentPosition;
            set
            {
                if (FileReader != null)
                {
                    _currentPosition = value;
                    if (CurrentFile != default)
                    {
                        FileReader.Position = value;
                    }
                }
                
            }
        }

        public TimeSpan CurrentTime
        {
            get => _currentTime;
            set
            {
                if (FileReader != default)
                {
                    _currentTime = value;
                    if (CurrentFile != default)
                    {
                        FileReader.CurrentTime = value;
                    }
                }
                
            }
        }

        public int Volume
        {
            get => _volume;
            set
            {
                if (_volume != value)
                {
                    _volume = value;
                    waveOut.Volume = Volume / 100f;
                    VolumeState = (Volume == 0) ? VolumeState.Muted : (Volume <= 40) ? VolumeState.Low : (Volume <= 70) ? VolumeState.Medium : VolumeState.High;
                }
                RaisePropertyChanged();
            }
        }

        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                if (_isMuted != value)
                {
                    _isMuted = value;
                    if (IsMuted)
                    {
                        oldVolume = Volume;
                        Volume = 0;
                    }
                    else Volume = oldVolume;
                }
                RaisePropertyChanged();
            }
        }

        public VolumeState VolumeState { get => _volumeState; set { _volumeState = value; RaisePropertyChanged(); } }

        public bool IsPlaying { get => waveOut.PlaybackState == PlaybackState.Playing; }

        public bool IsStopped { get => waveOut.PlaybackState == PlaybackState.Stopped; }

        public ObservableCollection<AudioFile> Files { get; set; }

        private void SelectNextFile()
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
        }

        private void SelectPreviousFile()
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
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (FileReader != null)
            {
                CurrentTime = FileReader.CurrentTime;
                RaisePropertyChanged("CurrentTime");
                _currentPosition = FileReader.Position;
                RaisePropertyChanged("CurrentPosition");

                if (FileReader.Position == FileReader.Length)
                {
                    FileReader = null;
                    SelectNextFile();
                   
                }
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

        public RelayCommand PausePlayCommand
        {
            get => new RelayCommand(() =>
            {
                if (CurrentFile != default)
                {
                    switch (waveOut.PlaybackState)
                    {
                        case PlaybackState.Stopped:
                            FileReader = new AudioFileReader(CurrentFile.Patch);
                            waveOut.Init(FileReader);
                            waveOut.Play();
                            break;
                        case PlaybackState.Playing:
                            waveOut.Pause();
                            break;
                        case PlaybackState.Paused:
                            waveOut.Play();
                            break;
                        default:
                            break;
                    }
                    RaisePropertyChanged("IsPlaying");
                    RaisePropertyChanged("IsStopped");
                }

            });
        }

        public RelayCommand StopRepeatCommand
        {
            get => new RelayCommand(() =>
            {
                switch (waveOut.PlaybackState)
                {
                    case PlaybackState.Stopped:
                        CurrentFile = Files.FirstOrDefault();
                        waveOut.Play();

                        break;
                    case PlaybackState.Playing:
                        waveOut.Stop();

                        break;
                    case PlaybackState.Paused:
                        waveOut.Stop();
                        break;
                    default:
                        break;
                }
                RaisePropertyChanged("IsPlaying");
                RaisePropertyChanged("IsStopped");

            });
        }

        public RelayCommand NextFileCommand => new RelayCommand(() => SelectNextFile());
        public RelayCommand PreviousFileCommand => new RelayCommand(() => SelectPreviousFile());

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

        public RelayCommand MuteCommand => new RelayCommand(() => oldVolume = Volume);
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
