using System;
using System.IO.Ports;
using System.Threading;

namespace SerialPortAPP
{
    public enum TYPE
    {
        LISTENER,
        WRITER
    }

    class SerialPortObject : SerialPort, IDisposable
    {

        // TODO manage errors
        // public event System.IO.Ports.SerialErrorReceivedEventHandler ErrorReceived;
        //  Frame[8] - Le matériel a détecté une erreur de trame.
        //  Overrun[2] - Un dépassement de mémoire tampon de caractères s’est produit. Le caractère suivant est perdu.
        //  RXOver[1] - Un dépassement de la mémoire tampon d’entrée s’est produit. Il n’y a plus de place dans la mémoire tampon d’entrée ou un caractère a été reçu après le caractère de fin de fichier.
        //  RXParity[4] - Le matériel a détecté une erreur de parité.
        //  TXFull[256] - L’application a essayé de transmettre un caractère, mais la mémoire tampon de sortie était pleine.


        const byte SEPARATOR = (byte)0x0D; // saut de ligne (CR)
        Random rnd = new Random();
        CancellationTokenSource cts = new CancellationTokenSource();
        ManualResetEvent manualResetEvent = new ManualResetEvent(true);
        bool _isEnabled = false;

        // To detect redundant calls
        private bool _disposed = false;

        public SerialPortObject() { }

        ~SerialPortObject() => Dispose(false);

        public SerialPortObject(string portName, TYPE type) : base(portName)
        {
            try
            {
                this.Open();
            } catch (Exception ex) {
                Console.WriteLine("Error opening my port: {0}", ex.Message);
            }
            
            if (IsOpen)
            {
                _isEnabled = true;
                if (type.Equals(TYPE.LISTENER))
                {
                    this.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                }
                else
                {
                    new Thread(DataSentThread).Start(cts.Token);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (this.IsOpen) this.Close();
            }

            cts.Cancel();

            _disposed = true;
        }

        public Action<String> messageSent;
        public void DataSentThread(object obj)
        {
            CancellationToken token = (CancellationToken)obj;
            while (true)
            {
                int number = rnd.Next(1, 65535);
                if (messageSent != null)
                {
                    this.WriteLine(number.ToString("D5") + (char)0x0D);
                    messageSent(number.ToString("D5"));
                }

                Thread.Sleep(500);
                if (token.IsCancellationRequested) return;
                manualResetEvent.WaitOne();
            }
        }

        public void PauseThreadSend()
        {
            if (_isEnabled && IsOpen) manualResetEvent.Reset();
        }


        public void ResumeThreadSend()
        {
            if (_isEnabled && IsOpen) manualResetEvent.Set();
        }

        public Action<String> messageReceived;

        string newDataAvailable = String.Empty;
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort serial = (SerialPort)sender;
            while (serial.IsOpen && serial.BytesToRead > 0)
            {
                byte data = (byte)serial.ReadByte();

                if (data == SEPARATOR)
                {
                    if (this.newDataAvailable.Equals("")) return;
                    if (messageReceived != null)
                    {
                        messageReceived(newDataAvailable);
                        this.newDataAvailable = String.Empty;
                    }
                }
                else
                {
                    if (data >= 0x30 && data <= 0x39)
                    {
                        newDataAvailable += Convert.ToChar(data);
                    }
                }
            }
        }
    }
}