﻿using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RusRoadLib
{
    // получение и обработка данных от датчика
    class DataSensorHandler : IDisposable
    {
        NetworkStream s;
        TcpClient tcpClient;
        byte[] FileBuf = new byte[1]; // место для сбора всего получаемого файла
        int Lbuf;
        int Timeout = 5000; //Величина тайм-аута
        RusRoadsData db = new RusRoadsData();
        private enum fields : int { Govnumber = 0, Highway_Id, Time, Speed };

        // Конструктор
        public DataSensorHandler(TcpClient c)
        {
            s = c.GetStream();
            Lbuf = c.ReceiveBufferSize;
            tcpClient = c;
        }
        // Чтение данных
        public async Task ReadDataAsync(CancellationToken ct)
        {

            var buf = new byte[Lbuf];

            int count; int readpos = 0; int writepos = 0;
            while (true)
            {
                count = await s.ReadAsync(buf, 0, Lbuf);
                if (count <= 0) break;

                Array.Resize(ref FileBuf, readpos + count);
                for (int i = 0; i < count; i++)
                {
                    FileBuf[writepos + i] = buf[i];
                }
                readpos += count;
                writepos += count;
                LogExt.Message(string.Format("[Сервер :] прочитано {0} байт", readpos));
            }
        }
        // Чтение данных с тайм-аутом
        public async Task<bool> ReadWithTimeoutAsync()
        {
            using (var cts = new CancellationTokenSource())
            {
                var readTask = ReadDataAsync(cts.Token);
                var timeoutTask = Task.Delay(Timeout);
                await Task.WhenAny(readTask, timeoutTask);
                if (!readTask.IsCompleted)
                {
                    cts.Cancel(); // cancel read task
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        // Обработка принятых данных
        public async Task HandlerDataAsync()
        {
            using (StringReader text = new StringReader(Encoding.UTF8.GetString(FileBuf)))
            {
                {
                    while (true)
                    {
                        string str = await text.ReadLineAsync();
                        if (str == null) { break; }
                        Console.WriteLine(str);
                        await WritePassageAsync(str);
                    }
                }
            }
        }
        // Формирование полей и запись в БД сущности Проезд
        private async Task WritePassageAsync(string str)
        {
            string[] items = str.Split(new char[] { ',' });
            var p1 = new Passage();
            string w = items[(int)fields.Govnumber];
            //var cars = from c in db.CarOwner
            //           where c.Govnumber == w
            //           select new { c.CarOwner_Id };
            //if (cars.Count() > 0)
            //{
            //    p1.CarOwner_Id = cars.ToList()[0].CarOwner_Id;
            //}
            //else
            //{
            //    string mes = LogExt.ErrorMes(str, "Госномер " + w + " отсутствует в системе", null);
            //    LogExt.Message(mes, LogExt.MesLevel.Error);
            //    return;
            //};
            DateTime dt = new DateTime();
            w = items[(int)fields.Time];
            if (DateTime.TryParse(w, out dt))
            {
                p1.Time = dt;
            }
            else
            {
                string mes = LogExt.ErrorMes(str, "Не правильный формат даты " + w, null);
                LogExt.Message(mes, LogExt.MesLevel.Error);
                return;
            }
            p1.Highway_Id = int.Parse(items[(int)fields.Highway_Id]);
            p1.Speed = int.Parse(items[(int)fields.Speed]);
            var pps = db.Passage.Add(p1);


            try
            {

                await db.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                db.Passage.Local.Remove(pps);
                string mes = LogExt.ErrorMes(str, "Другая ошибка ", ex);
                LogExt.Message(mes, LogExt.MesLevel.Error);
                
                //Console.WriteLine("Сообщение : "+ex.Message);

            }
        }

        public void Dispose()
        {
            s.Dispose();
        }
        // Запись полученных данных в файл только для отладки
        public void WriteFile()
        {
            using (FileStream fstream = new FileStream(@"e:\_CSharp\Project\ClientServerOKR\Счет2.pdf", FileMode.Create))
            {
                fstream.Write(FileBuf, 0, FileBuf.Length);
            }
        }
    }
}