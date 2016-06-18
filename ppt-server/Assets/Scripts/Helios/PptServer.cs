namespace Helios
{
    using System;
    using System.Text;
    using System.Collections.Generic;
    using uPLibrary.Networking.M2Mqtt;
    using uPLibrary.Networking.M2Mqtt.Messages;

    public sealed class PptServer : IDisposable
    {
        private List<PptStreamer>   streamClients;
        private MqttClient          mqttClient;

        public const string         ChannelInBound  = "/pptin";
        public const string         ChannelOutBound = "/pptout";

        public PptServer(string broker)
        {
            streamClients = new List<PptStreamer>();

            mqttClient = new MqttClient(broker);
            mqttClient.MqttMsgPublishReceived += (sender, ev) =>
            {
                if (ev.Topic == ChannelInBound)
                    try { MessageReceived(Encoding.UTF8.GetString(ev.Message)); }
                    catch (Exception) { /* no-op */ }
            };
            mqttClient.Connect("ppt");
            mqttClient.Subscribe(
                new string[]{ ChannelInBound },
                new byte[]{ MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        private void MessageReceived(string message)
        {
            if (message.StartsWith("Add"))
            {
                PptStreamer.LaunchOptions opts = new PptStreamer.LaunchOptions
                {
                    /* TO DO */
                };

                PptStreamer streamer = new PptStreamer(opts);
                streamClients.Add(streamer);

                mqttClient.Publish(ChannelOutBound, Encoding.UTF8.GetBytes(streamer.GetHashCode().ToString()));
            }
            else
            if(message.StartsWith("Remove"))
            {
                string[] args = message.Split('!');

                PptStreamer streamer = streamClients.Find((client) =>
                {
                    return client.GetHashCode().ToString() == args[1];
                });

                if (streamer != null)
                {
                    streamer.Dispose();
                    streamClients.Remove(streamer);
                }
            }
        }

        #region IDisposable Support
        private bool disposed = false;

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (mqttClient != null && mqttClient.IsConnected)
                        mqttClient.Disconnect();

                    foreach (PptStreamer streamer in streamClients)
                        streamer.Dispose();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
