using System;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;

namespace AgDiag.Protocol
{
    public class UDP
    {
        private readonly PGNs _pgns;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private int cntr;

        public UDP(PGNs pgns)
        {
            _pgns = pgns;
        }

        public event EventHandler<int> DefaultSendsUpdated;

        public void LoadLoopback()
        {
            var cancellationToken = _cancellationTokenSource.Token;
            Task.Factory.StartNew(() => ReceiveLoopAsync(cancellationToken), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void CloseLoopback()
        {
            _cancellationTokenSource.Cancel();
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (UdpClient udpClient = new UdpClient(17777))
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var result = await udpClient.ReceiveAsync().ConfigureAwait(false);

                        HandleMessage(result.Buffer);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("UDP Error: " + ex.Message, "UDP Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HandleMessage(byte[] data)
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
    }
}
