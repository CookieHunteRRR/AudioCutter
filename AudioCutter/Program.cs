using NAudio.Wave;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;

namespace AudioCutter
{
    internal class Program
    {
        static double cutLengthInSeconds = 10d;

        static void Main(string[] args)
        {
            Console.WriteLine($"Введите время отрезка в секундах (по умолчанию {cutLengthInSeconds} секунд): ");
            try
            {
                cutLengthInSeconds = Int32.Parse(Console.ReadLine());
            }
            catch (Exception)
            {
                Console.WriteLine("Было введено некорректное время. Будет использовано время по умолчанию.\n");
            }

            while (true)
            {
                Console.WriteLine("Введите путь к MP3/WAV файлу:");
                string input = Console.ReadLine();
                string ext = Path.GetExtension(input);

                if (ext == ".mp3")
                {
                    TrimMP3(input);
                }
                else if (ext == ".wav")
                {
                    TrimWAV(input);
                }
                else
                {
                    Console.WriteLine("Некорректный формат или путь файла.");
                }
            }
        }

        public static void TrimWAV(string inputPath)
        {
            string inputFileName = Path.GetFileName(inputPath);
            string outputFolderName = $"output_{inputFileName}";
            var outputFolderInfo = Directory.CreateDirectory(outputFolderName);

            using (var reader = new AudioFileReader(inputPath))
            {
                int cutCount = 0;
                bool isFileOver = false;
                var cutAsTimeSpan = TimeSpan.FromSeconds(cutLengthInSeconds);

                while (!isFileOver)
                {
                    CreateNewWAVCut(outputFolderInfo.FullName, inputFileName, cutCount, reader.Take(cutAsTimeSpan));

                    cutCount++;
                    // Если следующий отрезок выйдет за длительность файла
                    if (reader.CurrentTime + cutAsTimeSpan > reader.TotalTime)
                    {
                        cutAsTimeSpan = reader.TotalTime - reader.CurrentTime;
                        isFileOver = true;
                    }
                }
                CreateNewWAVCut(outputFolderName, inputFileName, cutCount, reader.Take(cutAsTimeSpan));
                Console.WriteLine($"Файл порезан");
            }
        }

        public static void TrimMP3(string inputPath)
        {
            string inputFileName = Path.GetFileName(inputPath);
            string outputFolderName = $"output_{inputFileName}";
            var outputFolderInfo = Directory.CreateDirectory(outputFolderName);
            using (var reader = new Mp3FileReader(inputPath))
            {
                // Продолжительность фрейма в милисекундах
                float frameTime = 0.052f;

                Mp3Frame frame;
                int savedFrames = 0;
                int cutCount = 0;

                FileStream nextCut = CreateNewMP3Cut(outputFolderInfo.FullName, inputFileName, cutCount);
                while ((frame = reader.ReadNextFrame()) != null)
                {
                    nextCut.Write(frame.RawData, 0, frame.RawData.Length);
                    savedFrames++;

                    if ((double)savedFrames * frameTime >= cutLengthInSeconds * 2) // я не знаю как это работает, но работает - не трожь
                    {
                        cutCount++;
                        savedFrames = 0;

                        nextCut.Close();
                        nextCut = CreateNewMP3Cut(outputFolderInfo.FullName, inputFileName, cutCount);
                    }
                }

                nextCut.Close();
                Console.WriteLine($"Файл порезан");
            }
        }

        private static void CreateNewWAVCut(string folderPath, string inputFileName, int cutCount, ISampleProvider cutAsSampleProvider)
        {
            string outputPath = folderPath + $"\\{inputFileName}_{cutCount}.wav";
            WaveFileWriter.CreateWaveFile16(outputPath, cutAsSampleProvider);
        }

        private static FileStream CreateNewMP3Cut(string folderPath, string inputFileName, int cutCount)
        {
            return new FileStream(folderPath + $"\\{inputFileName}_{cutCount}.mp3", FileMode.Create, FileAccess.Write);
        }
    }
}