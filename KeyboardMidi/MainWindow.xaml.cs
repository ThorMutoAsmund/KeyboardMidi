//using Commons.Music.Midi;
using CannedBytes.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

// References
// https://github.com/obiwanjacobi/midi.net/tree/master/Source/Samples/CannedBytes.Midi.Samples.SysExUtil
// https://users.cs.cf.ac.uk/Dave.Marshall/Multimedia/node158.html#:~:text=MIDI%20messages%20are%20used%20by%20MIDI%20devices%20to%20communicate%20with%20each%20other.&text=MIDI%20message%20includes%20a%20status,bits%20produce%2016%20possible%20channels).

namespace KeyboardMidi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private const string preferredOutput = "Teensy MIDI";
        
        private bool autoScroll = true;
        private MidiOutPort midiOut = null;
        private List<Key> keysDown = new List<Key>();
        private byte octave = 5;
        private DispatcherTimer timer;

        private string PreferredOutput => Properties.Settings.Default.PreferredOutput;

        public MainWindow()
        {
            InitializeComponent();
                        
            this.display.Text += "KeyboardMidi by Thor Muto Asmund (C) 2021\n";
            this.display.Text += "\n";

            this.fileMenu.SubmenuOpened += (sender, e) => FileMenuSubmenuOpened();
            this.closeOutputMenu.Click += (sender, e) => FileCloseOutput();
            this.exitMenu.Click += (sender, e) => ExitApplication();
            this.allNotesOffMenu.Click += (sender, e) => AllNotesOff();
            // Try connect
            OpenOutput(this.PreferredOutput);

            if (this.midiOut != null)
            {
                this.display.Text += $"Last used output device {this.PreferredOutput} opened\n";
            }
            else if (!String.IsNullOrEmpty(this.PreferredOutput))
            {
                this.display.Text += $"Waiting for output device to connect...\n";
            }

            // Start connect timer
            this.timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1f);
            timer.Tick += TryConnectPreferred;
            timer.Start();
        }

        private void FileMenuSubmenuOpened()
        {
            this.outputsMenu.Items.Clear();
            var menuMidiOutCaps = new MidiOutPortCapsCollection();

            foreach (var outCaps in menuMidiOutCaps)
            {
                var isChecked = this.midiOut != null && this.midiOut.Capabilities.Name == outCaps.Name;
                var item = new MenuItem()
                {
                    Header = outCaps.Name,
                    IsCheckable = true,
                    IsChecked = isChecked
                };
                item.Click += (sender, e) =>
                {
                    if (!OpenOutput(outCaps.Name))
                    {
                        MessageBox.Show($"Output {outCaps.Name} not available", "Output error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                };
                this.outputsMenu.Items.Add(item);
            }
            if (this.outputsMenu.Items.Count == 0)
            {
                var item = new MenuItem()
                {
                    Header = "None",
                    IsCheckable = false,
                    FontStyle = FontStyles.Italic
                };
                this.outputsMenu.Items.Add(item);
            }
        }

        private void TryConnectPreferred(object sender, EventArgs e)
        {
            if (this.midiOut != null)
            {
                var deviceDisconnected = false;

                if (this.midiOut.Status == MidiPortStatus.Closed)
                {
                    deviceDisconnected = true;
                }
                else
                {
                    var menuMidiOutCaps = new MidiOutPortCapsCollection();

                    // Accessing this.midiOut.Capabilities on a disconnected (but not yet closed) midiOut throws an exception
                    try
                    {
                        if (this.midiOut.Capabilities == null || !menuMidiOutCaps.Any(oc => oc.Name == this.midiOut.Capabilities.Name))
                        {
                            deviceDisconnected = true;
                        }
                        else
                        {
                            return;
                        }
                    }
                    catch (Exception)
                    {
                        deviceDisconnected = true;
                    }
                }

                if (deviceDisconnected)
                {
                    this.display.Text += "Device disconnected\n";
                    this.midiOut = null;
                }
            }

            OpenOutput(this.PreferredOutput);
        }

        private void FileCloseOutput()
        {
            Properties.Settings.Default.PreferredOutput = null;
            Properties.Settings.Default.Save();

            CloseOutput();
        }

        private void CloseOutput()
        {
            if (this.midiOut != null)
            {
                this.midiOut.Close();
                this.midiOut = null;
            }
        }

        private void ExitApplication()
        {
            CloseOutput();
            Application.Current.Shutdown();
        }

        private void AllNotesOff()
        {
            if (this.midiOut != null)
            {
                for (int c = 0; c < 16; ++c)
                {
                    for (int note = 0; note < 128; ++note)
                    {
                        var midiData = new MidiData()
                        {
                            Status = (byte)(128+c),
                            Parameter1 = (byte)(note),
                            Parameter2 = 0
                        };

                        this.midiOut.ShortData(midiData);
                    }
                }
            }
        }

        private bool OpenOutput(string name)
        {
            var menuMidiOutCaps = new MidiOutPortCapsCollection();
            var index = menuMidiOutCaps.IndexOf(menuMidiOutCaps.FirstOrDefault(oc => oc.Name == name));

            if (index == -1)
            {
                return false;
            }

            CloseOutput();

            this.midiOut = new MidiOutPort();
            this.midiOut.Open(index);

            Properties.Settings.Default.PreferredOutput = name;
            Properties.Settings.Default.Save();

            this.display.Text += $"New output {name} selected\n";

            return true;
        }

        private void MainKeyDown(object sender, KeyEventArgs e)
        {
            if (this.midiOut != null)
            {
                ReadMidiKey(e.Key, true);
            }
        }
        private void MainKeyUp(object sender, KeyEventArgs e)
        {
            if (this.midiOut != null)
            {
                ReadMidiKey(e.Key, false);
            }
        }

        private void ReadMidiKey(Key key, bool on)
        {
            var keyAlreadyPressed = this.keysDown.Contains(key);

            if (on)
            {
                if (keyAlreadyPressed)
                {
                    return;
                }
                this.keysDown.Add(key);
            }
            else
            {
                this.keysDown.Remove(key);
            }

            switch (key)
            {
                case Key.D1: SendNote(59, on); break;
                case Key.Q: SendNote(60, on); break;
                case Key.D2: SendNote(61, on); break;
                case Key.W: SendNote(62, on); break;
                case Key.D3: SendNote(63, on); break;
                case Key.E: SendNote(64, on); break;
                case Key.R: SendNote(65, on); break;
                case Key.D5: SendNote(66, on); break;
                case Key.T: SendNote(67, on); break;
                case Key.D6: SendNote(68, on); break;
                case Key.Y: SendNote(69, on); break;
                case Key.D7: SendNote(70, on); break;
                case Key.U: SendNote(71, on); break;
                case Key.I: SendNote(72, on); break;
                case Key.D9: SendNote(73, on); break;
                case Key.O: SendNote(74, on); break;
                case Key.D0: SendNote(75, on); break;
                case Key.P: SendNote(76, on); break;

                //case Key.D1: SendNoteOn(59, down); break;
                case Key.Z: SendNote(48, on); break;
                case Key.S: SendNote(49, on); break;
                case Key.X: SendNote(50, on); break;
                case Key.D: SendNote(51, on); break;
                case Key.C: SendNote(52, on); break;
                case Key.V: SendNote(53, on); break;
                case Key.G: SendNote(54, on); break;
                case Key.B: SendNote(55, on); break;
                case Key.H: SendNote(56, on); break;
                case Key.N: SendNote(57, on); break;
                case Key.J: SendNote(58, on); break;
                case Key.M: SendNote(59, on); break;
                case Key.OemComma: SendNote(60, on); break;

                case Key.OemOpenBrackets: if (this.octave > 1) this.octave--; break;
                case Key.OemCloseBrackets: if (this.octave < 7) this.octave++; break;
            }
        }

        private void SendNote(byte note, bool on, byte velocity = 0x70)
        {
            try
            {
                if (this.midiOut != null)
                {
                    var midiData = new MidiData()
                    {
                        Status = on ? (byte)144 : (byte)128,
                        Parameter1 = (byte)(note + (octave - 5) * 12),
                        Parameter2 = velocity
                    };

                    this.midiOut.ShortData(midiData);
                    this.display.Text += $"0x{midiData.Status:x} 0x{midiData.Parameter1:x} 0x{midiData.Parameter2:x}\n";
                }
            }
            catch (Exception)
            {
                this.display.Text += "Error seding midi message. Maybe the output is in use by another program?\n";
            }
        }

        private void ScrollViewer_ScrollChanged(Object sender, ScrollChangedEventArgs e)
        {
            // User scroll event : set or unset auto-scroll mode
            if (e.ExtentHeightChange == 0)
            {   // Content unchanged : user scroll event
                if (this.scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
                {   // Scroll bar is in bottom
                    // Set auto-scroll mode
                    this.autoScroll = true;
                }
                else
                {   // Scroll bar isn't in bottom
                    // Unset auto-scroll mode
                    this.autoScroll = false;
                }
            }

            // Content scroll event : auto-scroll eventually
            if (this.autoScroll && e.ExtentHeightChange != 0)
            {   // Content changed and auto-scroll mode set
                // Autoscroll
                this.scrollViewer.ScrollToVerticalOffset(scrollViewer.ExtentHeight);
            }
        }
    }
}
