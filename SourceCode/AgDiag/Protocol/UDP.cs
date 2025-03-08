using System;
using System.Net.Sockets;
using System.Net;
using System.Windows.Forms;

namespace AgDiag.Protocol
{
    public class UDP
    {
        private readonly PGNs _pgns;

        private UdpClient _udpClient;

        private IPEndPoint epSender = new IPEndPoint(IPAddress.Any, 0);

        private int cntr;

        public UDP(PGNs pgns)
        {
            _pgns = pgns;
        }

        public event EventHandler<int> DefaultSendsUpdated;

        public void LoadLoopback()
        {
            try //loopback
            {
                _udpClient = new UdpClient(17777);

                _udpClient.BeginReceive(new AsyncCallback(ReceiveDataLoopAsync), null);
            }
            catch (Exception ex)
            {
                //lblStatus.Text = "Error";
                MessageBox.Show("Load Error: " + ex.Message, "UDP Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void CloseLoopback()
        {
            _udpClient?.Close();
        }

        //loopback functions
        private void ReceiveFromLoopBack(int port, byte[] data)
        {
            if (data[0] == 0x80 && data[1] == 0x81)
            {
                switch (data[3])
                {
                    case 253:
                        {
                            for (int i = 5; i < data.Length; i++)
                            {
                                _pgns.asModule.pgn[i] = data[i];
                            }

                            break;
                        }
                    case 254:
                        {

                            for (int i = 5; i < data.Length; i++)
                            {
                                _pgns.asData.pgn[i] = data[i];
                            }

                            break;
                        }
                    case 252:
                        {

                            for (int i = 5; i < data.Length; i++)
                            {
                                _pgns.asSet.pgn[i] = data[i];
                            }

                            break;
                        }
                    case 251:
                        {

                            for (int i = 5; i < data.Length; i++)
                            {
                                _pgns.asConfig.pgn[i] = data[i];
                            }

                            break;
                        }
                    case 239:
                        {

                            for (int i = 5; i < data.Length; i++)
                            {
                                _pgns.maData.pgn[i] = data[i];
                            }

                            break;
                        }

                    default:
                        {
                            DefaultSendsUpdated?.Invoke(this, data.Length);
                            break;
                        }
                }
            }
            else
            {
                cntr += data.Length;
                DefaultSendsUpdated?.Invoke(this, cntr);
            }
        }

        private void ReceiveDataLoopAsync(IAsyncResult asyncResult)
        {
            try
            {
                byte[] localMsg = _udpClient.EndReceive(asyncResult, ref epSender);

                _udpClient.BeginReceive(new AsyncCallback(ReceiveDataLoopAsync), null);

                // Update status through a delegate
                int port = epSender.Port;
                ReceiveFromLoopBack(port, localMsg);
            }
            catch (Exception)
            {
                //MessageBox.Show("ReceiveData Error: " + ex.Message, "UDP Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
