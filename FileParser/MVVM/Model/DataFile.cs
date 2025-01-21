using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper.Configuration.Attributes;

namespace FileParser.MVVM.Model
{
    public class DataFile : ObservableObject
    {
        private string? _name;
        private string? _distance;
        private string? _angle;
        private double _width;
        private double _height;
        private string? _isDefect;

        [Name("Name")] // Атрибуты для CSV файла
        public string? Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        [Name("Distance")]
        public string? Distance
        {
            get => _distance;
            set => SetProperty(ref _distance, value);
        }

        [Name("Angle")]
        public string? Angle
        {
            get => _angle;
            set => SetProperty(ref _angle, value);
        }

        [Name("Width")]
        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        [Name("Hegth")]
        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        [Name("IsDefect")]
        public string? IsDefect
        {
            get => _isDefect;
            set => SetProperty(ref _isDefect, value);
        }
    }
}