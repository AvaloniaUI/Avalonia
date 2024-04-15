namespace DataGridAsyncDemoMVVM
{
    public class RemoteOrDbDataItem
    {
        public RemoteOrDbDataItem(int id, string name, string str1, string str2, int int1, double double1)
        {
            Id = id;
            Name = name;
            Str1 = str1;
            Str2 = str2;
            Int1 = int1;
            Double1 = double1;
        }
        
        public int Id { get; }
        public double Double1 { get; set; }
        public int Int1 { get; set; }
        public string Name { get; set; }
        public string Str1 { get; set; }
        public string Str2 { get; set; }
    }
}