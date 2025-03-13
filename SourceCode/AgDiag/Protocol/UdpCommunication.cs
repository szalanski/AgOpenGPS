using System;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;

namespace AgDiag.Protocol
{
    public class UdpCommunication
    {
        private readonly Pgns _pgns;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private int cntr;

        public UdpCommunication(Pgns pgns)
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
                            Buffer.BlockCopy(data, 5, _pgns.asModule.Bytes, 5, data.Length - 5);
                            break;
                        }
                    case 254:
                        {
                            Buffer.BlockCopy(data, 5, _pgns.asData.Bytes, 5, data.Length - 5);
                            break;
                        }
                    case 252:
                        {
                            Buffer.BlockCopy(data, 5, _pgns.asSet.Bytes, 5, data.Length - 5);
                            break;
                        }
                    case 251:
                        {
                            Buffer.BlockCopy(data, 5, _pgns.asConfig.Bytes, 5, data.Length - 5);
                            break;
                        }
                    case 239:
                        {
                            Buffer.BlockCopy(data, 5, _pgns.maData.Bytes, 5, data.Length - 5);
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
