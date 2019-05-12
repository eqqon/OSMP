/*
 * You may use Open System Management Protocol and its Reference Implementation free of charge as long as you honor 
 * the protocol specification. You may not use, license, distribute or advertise the protocol or any derivations of 
 * it under a different name. 

 * The Open System Management Protocol is Copyright © by Eqqon GmbH

 * THE PROTOCOL AND ITS REFERENCE IMPLEMENTATION ARE PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN 
 * ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE PROTOCOL OR ITS REFERENCE 
 * IMPLEMENTATION OR THE USE OR OTHER DEALINGS IN THE PROTOCOL OR REFERENCE IMPLEMENTATION.
 */

using System;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Osmp;

namespace OsmpWpfTest
{

    public partial class MainWindow : Window
    {
        private static OsmpClient _client = new OsmpClient() { };
        public static OsmpClient WsClient { get { return _client; } }
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
            this.Closed += OnClosed;
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _client.Dispose();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnLoaded;
            _client.MessageSent += OnMessageSent;
            _client.MessageReceived += OnMessageReceived;
            _client.CmdStreamReceived += OnStreamReceived;
            _client.EventsReceived += OnEventsReceived;
            _client.Error += OnError;
        }

        private void OnError(string msg, Exception e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (e == null)
                    ProtocolListingTextBox.AppendText("\nERROR: " + msg + "\n");
                else
                    ProtocolListingTextBox.AppendText("\nERROR: " + msg + "\n\t" + e.Message + "\n" + e.StackTrace + "\n");
                ProtocolListingTextBox.ScrollToEnd();
            }));
        }

        private void OnEventsReceived(OsmpEvent[] events)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var ev in events)
                    ProtocolListingTextBox.AppendText("\nGOT EVENT: " + ev.Id + ": " +
                                                      JsonConvert.SerializeObject(ev.Data) + "\n");
                ProtocolListingTextBox.ScrollToEnd();
            }));
        }


        private void OnMessageSent(string obj, bool success)
        {
            if (obj == null)
                return;
            var failed = success ? "" : " FAILED";
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                ProtocolListingTextBox.AppendText("\nTX" + failed + ":\n" + obj + "\n");
                ProtocolListingTextBox.ScrollToEnd();
            }));
        }

        private void OnMessageReceived(string obj)
        {
            if (obj == null)
                return;
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                ProtocolListingTextBox.AppendText("\nRX: " + obj + "\n");
                ProtocolListingTextBox.ScrollToEnd();
            }));

        }

        private void OnStreamReceived(OsmpStream stream)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                ProtocolListingTextBox.AppendText("\nGOT STREAM: Cmd=" + stream.Id + " " + JsonConvert.SerializeObject(stream.Data) + "\n");
                ProtocolListingTextBox.ScrollToEnd();
            }));

        }
        

        private async void OnConnect(object sender, RoutedEventArgs e)
        {
            if (_client.IsConnected)
                _client.Disconnect();
            _client.Address = AddressTextBox.Text;
            await _client.Connect();
            StatusTextBlock.Text = _client.WebSocket.ReadyState.ToString();
        }

        private void LoadMessage(OsmpMessage msg)
        {
            GenericTest.JsonTextBox.Text = JsonConvert.SerializeObject(msg);
        }

        private void LoadMessage(JObject j_object)
        {
            GenericTest.JsonTextBox.Text = JsonConvert.SerializeObject(j_object);
        }

        private void OnCallCreate(object sender, RoutedEventArgs e)
        {
            //LoadMessage(new JObject { { "type", "cmd" }, { "id", "vcall-create" }, 
            //    { "data", new JObject {
            //        { "call-id", "testcall1" }, 
            //        { "sources", new JArray("EvacuationText") }, 
            //        { "zones", new JArray("Floor 3", "Floor 4", "Lobby", "Staircase") }, 
            //        { "priority", 42 }, 
            //        { "owner", "Workstation 1"}
            //    }}
            //});
            LoadMessage(Vd1.VCallCreate("testcall1", new string[] {"EvacuationText"},
                new string[] {"Floor 3", "Floor 4", "Lobby", "Staircase"}, 42, "Workstation 1"));
        }

        private void OnCallPlay(object sender, RoutedEventArgs e)
        {
            LoadMessage(new JObject { { "type", "cmd" }, { "id", "vcall-play" }, 
                { "data", new JObject {
                    { "call-id", "testcall1" }, 
                }}
            });
        }

        private void OnCallStop(object sender, RoutedEventArgs e)
        {
            LoadMessage(new JObject { { "type", "cmd" }, { "id", "vcall-stop" }, 
                { "data", new JObject {
                    { "call-id", "testcall1" }, 
                }}
            });
        }

        private void OnCallList(object sender, RoutedEventArgs e)
        {
            LoadMessage(new JObject { { "type", "cmd" }, { "id", "vcall-list" }, });
        }

        private void OnCallStatus(object sender, RoutedEventArgs e)
        {
            LoadMessage(new JObject { { "type", "cmd" }, { "id", "vcall-status" }, 
                { "data", new JObject {
                    { "call-id", "testcall1" }, 
                }}
            });
        }

        private void OnCallDelete(object sender, RoutedEventArgs e)
        {
            LoadMessage(new JObject { { "type", "cmd" }, { "id", "vcall-delete" }, 
                { "data", new JObject {
                    { "call-id", "*" }, 
                }}
            });
        }

        private void OnEventList(object sender, RoutedEventArgs e)
        {
            LoadMessage(new JObject { { "type", "cmd" }, { "id", "event-list" }, });
        }

        private void OnEventSub(object sender, RoutedEventArgs e)
        {
            LoadMessage(new JObject { { "type", "cmd" }, { "id", "event-subscribe" },
                { "data", new JObject {
                    { "event", "*" },
                }}
            });
        }

        private void OnEventUnsub(object sender, RoutedEventArgs e)
        {
            LoadMessage(new JObject { { "type", "cmd" }, { "id", "event-unsubscribe" },
                { "data", new JObject {
                    { "event", "*" },
                }}
            });
        }

        private void OnDeviceStatus(object sender, RoutedEventArgs e)
        {
            LoadMessage(Vd1.VDeviceStatus(device_id:"*", ignore_not_installed:true, include_filter:@"lo\.64", exclude_filter:@"\.(pr|lr)\."));
        }

        private void OnDeviceStatusFilter(object sender, RoutedEventArgs e)
        {
            LoadMessage(Vd1.VDeviceStatusFilter(ignore_not_installed: true, include_filter: null, exclude_filter: @"\.(pr|lr)\."));
        }

    }
}
