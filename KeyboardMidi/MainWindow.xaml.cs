//using Commons.Music.Midi;
using CannedBytes.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KeyboardMidi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private const string preferredOutput = "Teensy MIDI";
        
        private bool autoScroll = true;
        private MidiOutPortCapsCollection midiOutCaps;
        private MidiOutPort output = null;
        private List<Key> keysDown = new List<Key>();
        private byte octave = 5;

        public MainWindow()
        {
            InitializeComponent();
                        
            this.display.Text += "KeyboardMidi by Thor Muto Asmund (C) 2021\n";
            this.display.Text += "\n";

            this.midiOutCaps = new MidiOutPortCapsCollection();

            var preferredOutput = Properties.Settings.Default.PreferredOutput;

            foreach (var outCaps in midiOutCaps)
            {
                this.display.Text += $"{midiOutCaps.IndexOf(outCaps)}: {outCaps.Name}\n";
                var isPreferred = outCaps.Name.Equals(preferredOutput);
                if (isPreferred)
                {
                    this.output = new MidiOutPort();
                    output.Open(midiOutCaps.IndexOf(outCaps));
                }
                var item = new MenuItem()
                {
                    Header = outCaps.Name,
                    IsCheckable = true,
                    IsChecked = isPreferred
                };
                item.Click += (sender, e) => OpenOutput(midiOutCaps.IndexOf(outCaps), item);
                this.outputsMenu.Items.Add(item);
            }

            this.display.Text += output == null ? "Select output\n" : $"Preferred output {preferredOutput} selected\n";

            this.closeOutputMenu.Click += (sender, e) => CloseOutput();
            this.exitMenu.Click += (sender, e) => System.Windows.Application.Current.Shutdown();
        }

        private void CloseOutput()
        {
            if (this.output != null)
            {
                this.output.Close();
                this.output = null;
            }
        }

        private void OpenOutput(int index, MenuItem selectedItem)
        {
            if (this.output != null)
            {
                this.output.Close();
            }
            this.output = new MidiOutPort();
            output.Open(index);

            foreach (var item in this.outputsMenu.Items)
            {
                if (item is MenuItem menuItem)
                {
                    menuItem.IsChecked = item == selectedItem;
                }
            }

            var name = midiOutCaps[index].Name;
            Properties.Settings.Default.PreferredOutput = name;
            Properties.Settings.Default.Save();
        }

        private void MainKeyDown(object sender, KeyEventArgs e)
        {
            if (output != null)
            {
                ReadMidiKey(e.Key, true);
            }
        }
        private void MainKeyUp(object sender, KeyEventArgs e)
        {
            if (output != null)
            {
                ReadMidiKey(e.Key, false);
            }
        }

        private void ReadMidiKey(Key key, bool down)
        {
            var keyAlreadyPressed = this.keysDown.Contains(key);

            if (down)
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
                case Key.D1: SendNoteOn(59, down); break;
                case Key.Q: SendNoteOn(60, down); break;
                case Key.D2: SendNoteOn(61, down); break;
                case Key.W: SendNoteOn(62, down); break;
                case Key.D3: SendNoteOn(63, down); break;
                case Key.E: SendNoteOn(64, down); break;
                case Key.R: SendNoteOn(65, down); break;
                case Key.D5: SendNoteOn(66, down); break;
                case Key.T: SendNoteOn(67, down); break;
                case Key.D6: SendNoteOn(68, down); break;
                case Key.Y: SendNoteOn(69, down); break;
                case Key.D7: SendNoteOn(70, down); break;
                case Key.U: SendNoteOn(71, down); break;
                case Key.I: SendNoteOn(72, down); break;
                case Key.D9: SendNoteOn(73, down); break;
                case Key.O: SendNoteOn(74, down); break;
                case Key.D0: SendNoteOn(75, down); break;
                case Key.P: SendNoteOn(76, down); break;

                //case Key.D1: SendNoteOn(59, down); break;
                case Key.Z: SendNoteOn(48, down); break;
                case Key.S: SendNoteOn(49, down); break;
                case Key.X: SendNoteOn(50, down); break;
                case Key.D: SendNoteOn(51, down); break;
                case Key.C: SendNoteOn(52, down); break;
                case Key.V: SendNoteOn(53, down); break;
                case Key.G: SendNoteOn(54, down); break;
                case Key.B: SendNoteOn(55, down); break;
                case Key.H: SendNoteOn(56, down); break;
                case Key.N: SendNoteOn(57, down); break;
                case Key.J: SendNoteOn(58, down); break;
                case Key.M: SendNoteOn(59, down); break;
                case Key.OemComma: SendNoteOn(60, down); break;

                case Key.OemOpenBrackets: if (this.octave > 1) this.octave--; break;
                case Key.OemCloseBrackets: if (this.octave < 7) this.octave++; break;
            }
        }

        private void SendNoteOn(byte note, bool down, byte velocity = 0x70)
        {
            try
            {
                if (output != null)
                {
                    var midiData = new MidiData()
                    {
                        Status = down ? (byte)144 : (byte)128,
                        Parameter1 = (byte)(note + (octave - 5) * 12),
                        Parameter2 = velocity
                    };

                    this.output.ShortData(midiData);
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
                if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
                {   // Scroll bar is in bottom
                    // Set auto-scroll mode
                    autoScroll = true;
                }
                else
                {   // Scroll bar isn't in bottom
                    // Unset auto-scroll mode
                    autoScroll = false;
                }
            }

            // Content scroll event : auto-scroll eventually
            if (autoScroll && e.ExtentHeightChange != 0)
            {   // Content changed and auto-scroll mode set
                // Autoscroll
                scrollViewer.ScrollToVerticalOffset(scrollViewer.ExtentHeight);
            }
        }
    }
}
