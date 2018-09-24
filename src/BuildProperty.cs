namespace JenkinsPlug
{
    public class BuildProperty
    {
        public readonly string Name;
        public readonly string Value;

        public BuildProperty(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}