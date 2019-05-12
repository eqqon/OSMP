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
using System.Windows.Controls;
using Newtonsoft.Json;
using Osmp;

namespace OsmpWpfTest
{
    public partial class GenericTest : UserControl
    {
        public GenericTest()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            JsonTextBox.Text = "{\"type\":\"cmd\",\"id\":\"help\"}";
        }

        private async void OnSend(object sender, RoutedEventArgs e)
        {
            var client = MainWindow.WsClient;
            var text = JsonTextBox.Text;
            try
            {
                var msg=JsonConvert.DeserializeObject<OsmpMessage>(JsonTextBox.Text);
                var send_task = client.Send(msg);
                JsonTextBox.Text = JsonConvert.SerializeObject(msg, Formatting.Indented);
                var response = await send_task;
                if (!response.IsOk)
                {
                    ResponseTextBox.Text = response.Result;
                    return;
                }
                ResponseTextBox.Text = JsonConvert.SerializeObject(response, Formatting.Indented);
            }
            catch (Exception)
            {
                client.SendText(text);
            }
        }
    }
}
