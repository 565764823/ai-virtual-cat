namespace AiCat.Data.Migration
{
    /// <summary>
    /// <see cref="IMigration"/> 的不可变实现。
    /// </summary>
    public sealed class MigrationStep : IMigration
    {
        public int Version { get; }

        public string Name { get; }

        public string Description { get; }

        public string[] SqlStatements { get; }

        public MigrationStep(int version, string name, string description, string[] sqlStatements)
        {
            Version = version;
            Name = name;
            Description = description;
            SqlStatements = sqlStatements;
        }
    }
}
