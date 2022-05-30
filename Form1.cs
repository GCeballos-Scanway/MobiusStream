using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Diagnostics;

namespace Mobius_Stream
{
    public partial class Form1 : Form
    {
        // Inicjalizacja zmiennych lokalnych dostępnych pomiędzy wątkami
        public bool running = true;
        public bool paused = false;
        Thread thWork = null;
        String address = "localhost";


        public Form1(string addr, bool topBar)
        {            
            // Ustawienie w zmiennej lokalnej adresu przekazanego z pętli Main() przy tworzeniu instancji okna aplikacji
            address = addr;

            // Inicjalizacja i wyświetlenie okna aplikacji
            InitializeComponent();
            Status("Uruchomiono");

            // Wyczyszczenie opisów.
            label1.Text = "";
            label2.Text = "";
            label3.Text = "";
            label4.Text = "";
        }

        public void Status(string msg)
        {
            // Wyświetla komunikat na pasku tytułu
            Program.Log(msg);

            try
            {
                this.Invoke(new Action(() => this.Text = "Mobius Stream - " + msg));
            }
            catch (Exception e)
            {
                Program.Log(e.ToString());
            }
        }


        private void Form1_Load(object sender, EventArgs e)
        {            
            // Wykonuje się po załadowaniu pełnego okna aplikacji
            // Tworzy i odpala wątek kręcący pętlą Worker()
            thWork = new Thread(Worker);
            thWork.Start();
        }

        public void Worker()
        {
            // Główna pętla pracy programu

            // Inicjalizacja zmiennych
            var fpsSW1 = new Stopwatch();
            var fpsSW2 = new Stopwatch();
            var fpsSW3 = new Stopwatch();
            var fpsSW4 = new Stopwatch();
            var bpsSW = new Stopwatch();
            double fps = 0.0;

            TcpClient client = new TcpClient();
            NetworkStream stream = null;
            byte[] sendBytes;
            byte[] bytes = new byte[client.ReceiveBufferSize];
            string qual = "5";
            int retlen;
            int retries = 0;
            string returndata = "";
            string error = "";
         
            while (running)
            {
                try
                {
                    if (!client.Connected)
                    {
                        // Jeśli kilent TCP nie jest połączony, to podejmuje kolejną próbę
                        if (retries > 0) Status("Próba połączenia z " + address + "  [" + (retries + 1).ToString() + "]");
                        else Status("Próba połączenia z " + address);
                        client.Connect(address, 8082);                       
                        // Jeśli nie uda odnaleźć serwera to rzuca błędem i program przechodzi stąd bezpośrednio do sekcji 'catch'

                        stream = client.GetStream();
                        if (client.Connected) Status("Połączono z " + address);
                    }
                    if (paused)
                    {
                        Thread.Sleep(100);
                    }
                    else 
                    {
                        // Iteruje po 4 kamerach (RozwijakL, RozwijakP, Piec, Noże)
                        for (int cam = 1; cam <= 4; cam++)
                        {
                            bytes = new byte[client.ReceiveBufferSize];

                            // Pobiera wartość jakości dla odpowiedniej kamery
                            if (cam == 1) qual = numericUpDown1.Value.ToString();
                            if (cam == 2) qual = numericUpDown2.Value.ToString();
                            if (cam == 3) qual = numericUpDown3.Value.ToString();
                            if (cam == 4) qual = numericUpDown4.Value.ToString();

                            // Buduje zapytanie do serwera, np. "cam:1_6"
                            sendBytes = Encoding.ASCII.GetBytes("cam:" + cam.ToString() + "_" + qual);
                            bpsSW.Restart();

                            // Wysyła zapytanie do serwera
                            stream.Write(sendBytes, 0, sendBytes.Length);
                            // Odbiera dane od serwera i sprawdza ich długość w bajtach
                            retlen = stream.Read(bytes, 0, (int)client.ReceiveBufferSize);
                            // Oblicza prędkość transmisji i przekazuje do wyświetlenia
                            returndata = Encoding.ASCII.GetString(bytes, 0, retlen);

                            // Jeśli przyszło mniej niż 10 bajtów, to najprawdopodobniej kod błędu
                            if (retlen < 10)
                            {
                                // Parsuje dane do stringa.
                                error = Encoding.ASCII.GetString(bytes, 0, retlen);

                                // Jeśli to określony kod błędu (np. "err_1"), to wyświetla odpowiedni komunikat na pasku tytułu
                                if (error.Contains("err_"))
                                {
                                    Status("Serwer zgłosił nieznany błąd [" + error + "]");
                                    if (error.Substring(4) == "1") Status("Streaming wstrzymany: trwa przetwarzanie zdarzenia... [" + error + "]");
                                    if (error.Substring(4) == "2") Status("Serwer napotkał niezidentyfikowany problem [" + error + "]");
                                }

                                // Jeśli nie jest to konkretny kod błędu, to wyświetla całą treść otrzymanych danych na pasku tytułu
                                else Status("Błąd: " + error);

                                // Czeka 1 s aż może błąd ustąpi
                                Thread.Sleep(1000);
                            }
                            // Jeśli przyszło więcej niż 10 bajtów, to najprawdopodobniej klatka
                            else
                            {
                                if (client.Connected) Status("Połączono z " + address);

                                // Dla aktualnie przetwarzanej kamery:
                                // - sprawdza ile czasu minęlo od poprzedniej klatki i wyświetla w postaci klatkażu pod widokiem z danej kamery
                                // - zeruje licznik czasu
                                // - parsuje obiekt obrazka z danych przesłanych przez serwer
                                // - buduje treść statusu danej kamery, zawierający rozmiar otrzymanego zdjęcia, aktualny klatkaż i tryb wyświetlania okienka.

                                if (cam == 1)
                                {
                                    fps = (double)1000 / fpsSW1.ElapsedMilliseconds;
                                    fpsSW1.Restart();
                                    pictureBox1.Invoke(new Action(() => pictureBox1.Image = Image.FromStream(new MemoryStream(bytes))));
                                    label1.Invoke(new Action(() => label1.Text = String.Format("1: {0:0.0} kb/s {1:0.#} fps {2:0.#} ms ",
                                        (double)retlen / bpsSW.ElapsedMilliseconds, fps, fpsSW1.ElapsedMilliseconds) + pictureBox1.SizeMode.ToString()));
                                }
                                if (cam == 2)
                                {
                                    fps = (double)1000 / fpsSW2.ElapsedMilliseconds;
                                    fpsSW2.Restart();
                                    pictureBox2.Invoke(new Action(() => pictureBox2.Image = Image.FromStream(new MemoryStream(bytes))));
                                    label2.Invoke(new Action(() => label2.Text = String.Format("2: {0:0.0} kb/s {1:0.#} fps {2:0.#} ms ",
                                        (double)retlen / bpsSW.ElapsedMilliseconds, fps, fpsSW2.ElapsedMilliseconds) + pictureBox2.SizeMode.ToString()));
                                }
                                if (cam == 3)
                                {
                                    fps = (double)1000 / fpsSW3.ElapsedMilliseconds;
                                    fpsSW3.Restart();
                                    pictureBox3.Invoke(new Action(() => pictureBox3.Image = Image.FromStream(new MemoryStream(bytes))));
                                    label3.Invoke(new Action(() => label3.Text = String.Format("3: {0:0.0} kb/s {1:0.#} fps {2:0.#} ms ",
                                        (double)retlen / bpsSW.ElapsedMilliseconds, fps, fpsSW3.ElapsedMilliseconds) + pictureBox3.SizeMode.ToString()));
                                }
                                if (cam == 4)
                                {
                                    fps = (double)1000 / fpsSW4.ElapsedMilliseconds;
                                    fpsSW4.Restart();
                                    pictureBox4.Invoke(new Action(() => pictureBox4.Image = Image.FromStream(new MemoryStream(bytes))));
                                    label4.Invoke(new Action(() => label4.Text = String.Format("4: {0:0.0} kb/s {1:0.#} fps {2:0.#} ms ",
                                        (double)retlen / bpsSW.ElapsedMilliseconds, fps, fpsSW4.ElapsedMilliseconds) + pictureBox4.SizeMode.ToString()));
                                }
                            }
                        }
                        Thread.Sleep(5);
                    }
                }
                catch (Exception e)
                {
                    Program.Log("===== catch:\n" + e.ToString());
                    Program.Log(">>> HResult: " + e.HResult.ToString());

                    // Ponieważ zazwyczaj błąd związany jest z połączeniem, wyświetla odpowiedni komunikat na pasku
                    Status("Brak połączenia z sewerem");
                    retries += 1;
                }
            }

            // Zakończenie pętli while(running) spowoduje wyjście z aplikacji
            Application.Exit();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Obsługuje kliknięcie krzyżyka (zamknięcie okna aplikacji) - zatrzymuje pętlę while(running) w wątku Worker() i zamyka aplikację
            running = false;
            Application.ExitThread();
            Application.Exit();
        }




        // Obsługa zdarzeń związanych z dwuklikiem na obrazkach - przełączanie między 3 trybami wyświetlania (Zoom, Stretch, Center)
        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            if (pictureBox1.SizeMode == PictureBoxSizeMode.Zoom) pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            else if (pictureBox1.SizeMode == PictureBoxSizeMode.StretchImage) pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage;
            else pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
        }

        private void pictureBox2_DoubleClick(object sender, EventArgs e)
        {
            if (pictureBox2.SizeMode == PictureBoxSizeMode.Zoom) pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
            else if (pictureBox2.SizeMode == PictureBoxSizeMode.StretchImage) pictureBox2.SizeMode = PictureBoxSizeMode.CenterImage;
            else pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
        }

        private void pictureBox3_DoubleClick(object sender, EventArgs e)
        {
            if (pictureBox3.SizeMode == PictureBoxSizeMode.Zoom) pictureBox3.SizeMode = PictureBoxSizeMode.StretchImage;
            else if (pictureBox3.SizeMode == PictureBoxSizeMode.StretchImage) pictureBox3.SizeMode = PictureBoxSizeMode.CenterImage;
            else pictureBox3.SizeMode = PictureBoxSizeMode.Zoom;
        }

        private void pictureBox4_DoubleClick(object sender, EventArgs e)
        {
            if (pictureBox4.SizeMode == PictureBoxSizeMode.Zoom) pictureBox4.SizeMode = PictureBoxSizeMode.StretchImage;
            else if (pictureBox4.SizeMode == PictureBoxSizeMode.StretchImage) pictureBox4.SizeMode = PictureBoxSizeMode.CenterImage;
            else pictureBox4.SizeMode = PictureBoxSizeMode.Zoom;
        }
    }
}

