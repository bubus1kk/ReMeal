namespace Application.DTOs.Profile
{
    public sealed class ProfileKpiDto
    {
        public ProfileKpiDto(string title, string value, string caption)
        {
            Title = title;
            Value = value;
            Caption = caption;
        }

        public string Title { get; }

        public string Value { get; }

        public string Caption { get; }
    }
}
