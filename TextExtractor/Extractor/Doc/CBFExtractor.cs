namespace TextExtractor.Extractor.Doc
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Extensions;

    /// <summary>
    ///     Windows Compound Binary File Format Extractor. CBF is base format for .doc, .xls и .ppt
    /// </summary>
    public class CBFExtractor
    {
        private const string OleStorageName = "ObjectPool";

        private const uint ENDOFCHAIN = 0xFFFFFFFE;
        private const uint FREESECT = 0xFFFFFFFF;
        private readonly List<int> DIFAT = new List<int>();
        private int cDIFAT;
        private int cDir;
        private int cFAT;
        private uint cMiniFAT;
        protected byte[] data;
        private uint fDIFAT;
        private int fDir;
        private uint fMiniFAT;

        // Размеры FAT-сектора (1 << 9 = 512), Mini FAT-сектора (1 << 6 = 64) и максимальный
        // размер потока, который может быть записан в miniFAT'е.

        // Массив последовательности FAT-секторов и массив "файлов" файловой структуры файла
        private Dictionary<int, int> fatChains = new Dictionary<int, int>();
        private List<FatDirectory> fatEntries = new List<FatDirectory>();
        private bool isLittleEndian = true;

        // Массив последовательностей Mini FAT-секторов и весь Mini FAT нашего файла
        private byte[] miniFAT;
        private List<int> miniFATChains = new List<int>();
        private int miniSectorCutoff = 4096;
        private int miniSectorShift = 6;
        private int sectorShift = 9;

        // Версия (3 или 4), а также способ записи чисел (little-endian)
        private int version = 3;

        protected void Extract()
        {
            if (IsCBFDocument(BitConverter.ToString(data, 0, 8)) == false)
                throw new InvalidOperationException("Document does not have CBF part");

            ReadHeader();
            ReadDIFAT();
            ReadFATChains();
            ReadMiniFATChains();
            ReadDirectoryStructure();

            miniFAT = GetDirectoryContentByName(FatDirectory.RootDirectoryName, true);
        }

        private bool IsCBFDocument(string abSig)
        {
            var str = abSig.ToUpperInvariant().Replace("-", "");

            return str == "D0CF11E0A1B11AE1" || str == "0E11FC0DD0CF11E0";
        }

        protected byte[] GetDirectoryContentByName(string name, bool isRoot = false)
        {
            var entry = fatEntries.FirstOrDefault(x => x.Name == name);

            if (entry == null)
                throw new InvalidOperationException(string.Format("Can't get stream by name: {0} from CBF", name));

            byte[] content;

            if (entry.Size < miniSectorCutoff && isRoot == false)
                content = GetMinifatContent(entry.Offset);
            else
                content = GetFatContent(entry.Offset);

            return content.SkipAndTake(0, (int) entry.Size);
        }

        private byte[] GetMinifatContent(int startOffset)
        {
            var sectorSize = 1 << miniSectorShift;
            var stream = new List<byte>();
            var index = startOffset;

            do
            {
                var offset = index << miniSectorShift;

                stream.AddRange(miniFAT.SkipAndTake(offset, sectorSize));

                if (miniFATChains.Contains(index) == false)
                    break;

                index = miniFATChains[index];
            } while (true);

            return stream.ToArray();
        }

        private byte[] GetFatContent(int startOffset)
        {
            var sectorSize = 1 << sectorShift;
            var stream = new List<byte>();
            var index = startOffset;

            do
            {
                var offset = (index + 1) << sectorShift;

                stream.AddRange(data.SkipAndTake(offset, sectorSize));

                if (fatChains.ContainsKey(index) == false)
                    break;

                index = fatChains[index];
            } while (true);

            return stream.ToArray();
        }

        private void ReadHeader()
        {
            // Для начала узнаем как записаны данные в файле
            var uByteOrder = BitConverter.ToString(data, 0x1C, 2).ToUpperInvariant().Replace("-", "");
            // Что ж наверняка это будет little-endian запись, но на всякий случай проверим
            isLittleEndian = uByteOrder == "FEFF";

            // Версия 3 или 4 (4ую ни разу не встречал, но в документации она описана)
            version = data.ReadInt16(0x1A);

            // Смещения для FAT и miniFAT
            sectorShift = data.ReadInt16(0x1E);
            miniSectorShift = data.ReadInt16(0x20);
            miniSectorCutoff = data.ReadInt32(0x38);

            // Количество вхождений в директорию файла и смещения до первого описания в файле
            if (version == 4)
                cDir = data.ReadInt32(0x28);
            fDir = data.ReadInt32(0x30);

            // Количество FAT-секторов в файле
            cFAT = data.ReadInt32(0x2C);

            // Количество и позиция первого miniFAT-сектора последовательностей.
            cMiniFAT = data.ReadUInt32(0x40);
            fMiniFAT = data.ReadUInt32(0x3C);

            // Где лежат цепочки FAT-секторов и сколько таких цепочек.
            cDIFAT = data.ReadInt32(0x48);
            fDIFAT = data.ReadUInt32(0x44);
        }

        // Итак, DIFAT. DIFAT показывает в каких секторах файла лежат
        // описания цепочек FAT-секторов. Без этих цепочек мы не сможем
        // прочитать содержимое потоков в сильно "фрагментированных"
        // файлах
        private void ReadDIFAT()
        {
            int i;
            // Первые 109 ссылок на цепочки хранятся прямо в заголовке нашего файла
            for (i = 0; i < 109; i++)
                DIFAT.Add(data.ReadInt32(0x4C + i*4));

            // Там же мы смотрим, есть ли ещё где-нибудь ссылки на цепочки. В небольших
            // файлах (до 8,5 Мб) их нет (хватает первых 109 ссылок), в больших - мы
            // обязаны прочитать и их.
            if (fDIFAT != ENDOFCHAIN)
            {
                // Размер сектора и позиция откуда надо начинать читать ссылки.
                var size = 1 << sectorShift;
                var from = fDIFAT;
                var j = 0;

                do
                {
                    // Получаем позицию в файле с учётом заголовка
                    var start = ((int) (from + 1)) << sectorShift;
                    // Читаем ссылки на сектора цепочек
                    for (i = 0; i < (size - 4); i += 4)
                        DIFAT.Add(data.ReadInt32(start + i));
                    // Находим следующий DIFAT-сектор - ссылка на него
                    // записана последним "словом" в текущем DIFAT-секторе
                    from = data.ReadUInt32(start + i);
                    // Если сектор существует, то метнёмся к нему.
                } while (from != ENDOFCHAIN && ++j < cDIFAT);
            }

            // Для экономии удаляем конечные неиспользуемые ссылки.
            while (DIFAT.Last() == FREESECT)
                DIFAT.Remove(DIFAT.Last());
        }

        // Так, DIFAT мы прочитали - теперь нужно ссылки на цепочки FAT-секторов
        // превратить в реальные цепочки. Поэтому побегаем по файлу дальше.
        private void ReadFATChains()
        {
            // Размер сектора
            var size = 1 << sectorShift;

            // Обходим массив DIFAT.
            for (var i = 0; i < DIFAT.Count; i++)
            {
                // Идём по ссылке на нужный нам сектор (с учётом заголовка)
                var from = (DIFAT[i] + 1) << sectorShift;
                // Получаем цепочку FAT: индекс массива - это текущий сектор,
                // значение элемента массива - индекс следующего элемента или
                // ENDOFCHAIN - если это последний элемент цепочки.
                for (var j = 0; j < size; j += 4)
                    fatChains[fatChains.Count] = data.ReadInt32(from + j);
            }
        }

        // FAT-цепочки мы прочитали, теперь нужно прочитать MiniFAT-цепочки
        // абсолютно также.
        private void ReadMiniFATChains()
        {
            // Размер сектора
            var size = 1 << sectorShift;

            // Ищем первый сектор с MiniFAT-цепочками
            var from = fMiniFAT;
            // Если в файле MiniFAT используется, то 
            while (from != ENDOFCHAIN)
            {
                // находим смещение к сектору с MiniFat-цепочкой
                var start = (int) ((from + 1) << sectorShift);
                // Читаем цепочку из текущего сектора
                for (var i = 0; i < size; i += 4)
                    miniFATChains.Add(data.ReadInt32(start + i));
                // И если этот сектор не конечный в FAT-цепочке, то переходим дальше.
                from = fatChains.ContainsKey((int) from)
                           ? (uint) fatChains[(int) from]
                           : ENDOFCHAIN;
            }
        }

        private void ReadDirectoryStructure()
        {
            var index = fDir;
            var sectorSize = 1 << sectorShift;

            do
            {
                var offset = ((index + 1) << sectorShift);

                for (var i = 0; i < sectorSize; i += 128)
                {
                    var entry = data.SkipAndTake(offset + i, 128);

                    fatEntries.Add(new FatDirectory
                                       {
                                           Name = GetDirectoryName(entry),
                                           Type = entry[0x42],
                                           Color = entry[0x43],
                                           Left = entry.ReadInt32(0x44),
                                           Rigth = entry.ReadInt32(0x48),
                                           Child = entry.ReadInt32(0x4C),
                                           Offset = entry.ReadInt32(0x74),
                                           Size = entry.ReadInt64(0x78)
                                       });
                }

                if (fatChains.ContainsKey(index) == false)
                    break;

                index = fatChains[index];
            } while (true);

            var oleStorageDirectory = fatEntries.SingleOrDefault(x => x.Name == OleStorageName);

            if (oleStorageDirectory != null && oleStorageDirectory.Child != -1)
            {
                var removeDirectories = new List<FatDirectory>();

                NormalizeTree(fatEntries, fatEntries[oleStorageDirectory.Child], ref removeDirectories);

                foreach (var removeDirectory in removeDirectories)
                    fatEntries.Remove(removeDirectory);
            }

            while (fatEntries.Last().Type == 0)
                fatEntries.Remove(fatEntries.Last());
        }

        private void NormalizeTree(List<FatDirectory> fatDirectories, FatDirectory removeDirectory, ref List<FatDirectory> removeDirectories )
        {
            removeDirectories.Add(removeDirectory);
            
            if (removeDirectory.Left != -1)
                NormalizeTree(fatDirectories,fatDirectories[removeDirectory.Left], ref removeDirectories);

            if (removeDirectory.Rigth != -1)
                NormalizeTree(fatDirectories, fatDirectories[removeDirectory.Rigth], ref removeDirectories);

            if (removeDirectory.Child != -1)
                NormalizeTree(fatDirectories, fatDirectories[removeDirectory.Child], ref removeDirectories);
        }

        private string GetDirectoryName(byte[] directoryContent)
        {
            var nameSize = directoryContent.ReadInt16(0x40);
            var nameBytes = directoryContent.SkipAndTake(0, nameSize);
            var result = Encoding.GetEncoding("UTF-16").GetString(nameBytes);

            return result.Trim().Replace("\0", "");
        }
    }
}