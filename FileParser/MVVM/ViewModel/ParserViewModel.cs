using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OfficeOpenXml;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using FileParser.MVVM.Model;

namespace FileParser.MVVM.ViewModel
{
    public class ParserViewModel : ObservableObject
    {
        private ObservableCollection<DataFile>? _dataItems; // Список объектов данных
        private DataFile? _selectedItem; // Выбранный элемент из списка данных
        private string? _currentFilePath; // Путь текущего файла

        // Свойства для биндинга данных в представление
        public ObservableCollection<DataFile>? DataItems
        {
            get => _dataItems;
            set => SetProperty(ref _dataItems, value);
        }
        public DataFile? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }
        public string? CurrentFilePath
        {
            get => _currentFilePath;
            set => SetProperty(ref _currentFilePath, value);
        }

        // Команды для работы с файлами (открытие и сохранение)
        public AsyncRelayCommand SelectFileCommand { get; set; }
        public AsyncRelayCommand SaveFileCommand { get; set; }

        public ParserViewModel()
        {
            // Инициализация команд
            SelectFileCommand = new AsyncRelayCommand(SelectFile);
            SaveFileCommand = new AsyncRelayCommand(SaveFile);
        }

        // Метод для выбора файла
        private async Task SelectFile()
        {
            // Открытие диалога выбора файла
            var dialog = new OpenFileDialog
            {
                Filter = "Excel and CSV Files|*.xlsx;*.csv", // Ограничиваем типы файлов
                Title = "Выберите файл" // Заголовок диалога
            };

            try
            {
                // Если диалог завершен успешно, загружаем файл
                if (dialog.ShowDialog() == true)
                {
                    await LoadFile(dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибок при открытии файла
                MessageBox.Show($"Ошибка при открытии файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"Ошибка при открытии файла: {ex}"); // Логируем ошибку
            }
        }

        // Метод для загрузки файла
        private async Task LoadFile(string filePath)
        {
            try
            {
                CurrentFilePath = filePath; // Сохраняем путь к открытому файлу

                // Проверяем расширение файла и вызываем соответствующий метод загрузки
                if (Path.GetExtension(filePath).Equals(".xlsx"))
                {
                    await LoadExcelFile(filePath);
                }
                else if (Path.GetExtension(filePath).Equals(".csv"))
                {
                    await LoadCsvFile(filePath);
                }
            }
            catch (FileNotFoundException ex)
            {
                // Обработка ошибки, если файл не найден
                MessageBox.Show($"Файл не найден: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"Файл не найден: {ex}");
            }
            catch (UnauthorizedAccessException ex)
            {
                // Обработка ошибки, если нет доступа к файлу
                MessageBox.Show($"Нет доступа к файлу: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"Нет доступа к файлу: {ex}");
            }
            catch (Exception ex)
            {
                // Общая ошибка загрузки файла
                MessageBox.Show($"Ошибка загрузки файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"Ошибка загрузки файла: {ex}");
            }
        }

        // Метод для загрузки Excel файла
        private async Task LoadExcelFile(string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            try
            {
                // Загружаем Excel файл в фоновом потоке
                await Task.Run(() =>
                {
                    using var package = new ExcelPackage(new FileInfo(filePath));
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault(); // Открываем первый лист

                    if (worksheet != null)
                    {
                        var items = new ObservableCollection<DataFile>();

                        // Проходим по строкам и добавляем данные в коллекцию
                        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                        {
                            var data = new DataFile
                            {
                                Name = worksheet.Cells[row, 1].Text,
                                Distance = worksheet.Cells[row, 2].Text,
                                Angle = worksheet.Cells[row, 3].Text,
                                Width = double.TryParse(worksheet.Cells[row, 4].Text, out var width) ? width : 0,
                                Height = double.TryParse(worksheet.Cells[row, 5].Text, out var height) ? height : 0,
                                IsDefect = worksheet.Cells[row, 6].Text
                            };
                            items.Add(data); // Добавляем элемент в коллекцию
                        }

                        // Обновляем данные в главном потоке
                        App.Current.Dispatcher.Invoke(() => DataItems = items);
                    }
                });
            }
            catch (IOException ioEx)
            {
                // Ошибка ввода-вывода (например, файл используется другой программой)
                MessageBox.Show($"Ошибка при чтении Excel файла: {ioEx.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"Ошибка при чтении Excel файла: {ioEx}");
            }
            catch (FormatException formatEx)
            {
                // Ошибка форматирования данных (например, неверный формат данных в ячейке)
                MessageBox.Show($"Ошибка формата данных: {formatEx.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"Ошибка формата данных: {formatEx}");
            }
            catch (ArgumentNullException argEx)
            {
                // Ошибка, если аргумент был пустым или неверным
                MessageBox.Show($"Ошибка в аргументе: {argEx.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"Ошибка в аргументе: {argEx}");
            }
            catch (Exception ex)
            {
                // Общая ошибка загрузки Excel файла
                MessageBox.Show($"Ошибка при загрузке Excel файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"Ошибка при загрузке Excel файла: {ex}");
            }
        }

        // Метод для загрузки CSV файла
        private async Task LoadCsvFile(string filePath)
        {
            try
            {
                // Загружаем CSV файл в фоновом потоке
                await Task.Run(() =>
                {
                    using var reader = new StreamReader(filePath);
                    var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
                    {
                        Delimiter = ";" // Указываем, что разделитель точка с запятой
                    };
                    using var csv = new CsvHelper.CsvReader(reader, config);

                    var records = csv.GetRecords<DataFile>().ToList(); // Читаем данные из CSV

                    // Обновляем данные в главном потоке
                    App.Current.Dispatcher.Invoke(() => DataItems = new ObservableCollection<DataFile>(records));
                });
            }
            catch (CsvHelper.HeaderValidationException ex)
            {
                // Ошибка валидации заголовков
                Log.Error($"Ошибка валидации заголовков: {ex.Message}");
                MessageBox.Show("Ошибка валидации заголовков. Проверьте файл на соответствие формату.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (FileNotFoundException ex)
            {
                // Ошибка при отсутствии файла
                Log.Error($"Файл не найден: {ex.Message}");
                MessageBox.Show("Файл не найден. Проверьте путь к файлу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException ex)
            {
                // Ошибка ввода-вывода
                Log.Error($"Ошибка ввода-вывода: {ex.Message}");
                MessageBox.Show($"Ошибка ввода-вывода: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                // Общая ошибка при загрузке CSV файла
                Log.Error($"Ошибка при загрузке CSV файла: {ex.Message}");
                MessageBox.Show($"Ошибка при загрузке CSV файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для сохранения файла
        private async Task SaveFile()
        {
            if (DataItems == null || DataItems.Count == 0)
            {
                MessageBox.Show("Нет данных для сохранения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(CurrentFilePath))
            {
                try
                {
                    // Проверяем расширение файла и вызываем соответствующий метод сохранения
                    if (Path.GetExtension(CurrentFilePath).Equals(".xlsx"))
                    {
                        await SaveExcelFile(CurrentFilePath);
                    }
                    else if (Path.GetExtension(CurrentFilePath).Equals(".csv"))
                    {
                        await SaveCsvFile(CurrentFilePath);
                    }

                    MessageBox.Show("Файл успешно сохранен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (UnauthorizedAccessException ex)
                {
                    // Ошибка при доступе к файлу
                    MessageBox.Show($"Ошибка доступа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Log.Error($"Ошибка доступа при сохранении файла: {ex}");
                }
                catch (IOException ex)
                {
                    // Ошибка при вводе-выводе
                    MessageBox.Show($"Ошибка ввода-вывода при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Log.Error($"Ошибка ввода-вывода при сохранении: {ex}");
                }
                catch (Exception ex)
                {
                    // Общая ошибка сохранения файла
                    MessageBox.Show($"Ошибка сохранения файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Log.Error($"Ошибка при сохранении файла: {ex}");
                }
            }
            else
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Excel and CSV Files|*.xlsx;*.csv", // Ограничиваем типы файлов
                    Title = "Сохранить файл"
                };

                if (dialog.ShowDialog() == true)
                {
                    // Если пользователь выбрал путь, сохраняем файл
                    CurrentFilePath = dialog.FileName;
                    await SaveFile();
                }
            }
        }

        // Метод для сохранения Excel файла
        private async Task SaveExcelFile(string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            try
            {
                // Создаем новый файл Excel
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Data"); // Создаем новый лист

                // Записываем данные в ячейки
                for (int i = 0; i < DataItems.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = DataItems[i].Name;
                    worksheet.Cells[i + 2, 2].Value = DataItems[i].Distance;
                    worksheet.Cells[i + 2, 3].Value = DataItems[i].Angle;
                    worksheet.Cells[i + 2, 4].Value = DataItems[i].Width;
                    worksheet.Cells[i + 2, 5].Value = DataItems[i].Height;
                    worksheet.Cells[i + 2, 6].Value = DataItems[i].IsDefect;
                }

                // Сохраняем файл
                await Task.Run(() => package.SaveAs(new FileInfo(filePath)));
            }
            catch (DirectoryNotFoundException dirEx)
            {
                // Ошибка, если путь к директории не найден
                MessageBox.Show($"Директория не найдена: {dirEx.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"Директория не найдена: {dirEx}");
            }
            catch (IOException ioEx)
            {
                // Ошибка ввода-вывода (например, файл занят другим процессом)
                MessageBox.Show($"Ошибка при сохранении Excel файла: {ioEx.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"Ошибка при сохранении Excel файла: {ioEx}");
            }
            catch (ArgumentException argEx)
            {
                // Ошибка, если путь к файлу неверный или содержит недопустимые символы
                MessageBox.Show($"Ошибка аргумента пути файла: {argEx.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"Ошибка аргумента пути файла: {argEx}");
            }
            catch (Exception ex)
            {
                // Общая ошибка при сохранении Excel файла
                MessageBox.Show($"Ошибка при сохранении Excel файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"Ошибка при сохранении Excel файла: {ex}");
            }
        }

        // Метод для сохранения CSV файла
        private async Task SaveCsvFile(string filePath)
        {
            try
            {
                // Запускаем процесс записи в файл в фоновом потоке
                await Task.Run(() =>
                {
                    // Открываем StreamWriter для записи в CSV файл с использованием кодировки UTF-8
                    using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

                    // Настройка параметров для CsvHelper (разделитель - точка с запятой)
                    var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
                    {
                        Delimiter = ";" // Разделитель - точка с запятой
                    };

                    // Создаем экземпляр CsvWriter для записи в файл
                    using var csv = new CsvHelper.CsvWriter(writer, config);

                    // Записываем данные из коллекции DataItems в файл
                    csv.WriteRecords(DataItems);
                });
            }
            catch (IOException ex)
            {
                // Обработка ошибки ввода-вывода (например, файл занят другим процессом)
                MessageBox.Show($"Ошибка при сохранении CSV файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"Ошибка при сохранении CSV файла: {ex}");
            }
            catch (UnauthorizedAccessException ex)
            {
                // Обработка ошибки доступа (например, недостаточно прав для записи в файл)
                MessageBox.Show($"Ошибка доступа при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"Ошибка доступа при сохранении: {ex}");
            }
            catch (Exception ex)
            {
                // Общая ошибка при сохранении CSV файла
                MessageBox.Show($"Ошибка при сохранении CSV файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"Ошибка при сохранении CSV файла: {ex}");
            }
        }
    }
}

//Использование Message.Show() нарушает паттерн MVVM, поэтому лучше использовать др. способы уведомления. К сожалению сейчас нет времени для расширения данной системы