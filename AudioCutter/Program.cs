using NAudio.Wave;
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
                Console.WriteLine("Введите путь к MP3 файлу:");
                string input = Console.ReadLine();
                TrimMP3(input);
            }
        }

        public static void TrimMP3(string inputPath)
        {
            if (!File.Exists(inputPath))
            {
                Console.WriteLine("Путь не существует\n");
                return;
            }

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

                FileStream nextCut = CreateNewCut(outputFolderInfo.FullName, inputFileName, cutCount);
                while ((frame = reader.ReadNextFrame()) != null)
                {
                    nextCut.Write(frame.RawData, 0, frame.RawData.Length);
                    savedFrames++;

                    if ((double)savedFrames * frameTime >= cutLengthInSeconds * 2) // я не знаю как это работает, но работает - не трожь
                    {
                        cutCount++;
                        savedFrames = 0;

                        nextCut.Close();
                        nextCut = CreateNewCut(outputFolderInfo.FullName, inputFileName, cutCount);
                        Console.WriteLine($"Сохранено {cutCount}");
                    }
                }

                nextCut.Close();
                Console.WriteLine($"Файл порезан");
            }
        }

        private static FileStream CreateNewCut(string folderPath, string inputFileName, int cutCount)
        {
            return new FileStream(folderPath + $"\\{inputFileName}_{cutCount}.mp3", FileMode.Create, FileAccess.Write);
        }
    }
}