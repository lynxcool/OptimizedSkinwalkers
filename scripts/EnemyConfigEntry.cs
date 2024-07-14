namespace OptimizedSkinwalkers
{
    using BepInEx.Configuration;
    using System;
    using System.Text;

    public struct EnemyConfigEntry
    {
        public Type EnemyType;
        public ConfigEntry<bool> configEntry;
        public string cleanedName;

        private readonly bool defaultValue;

        public EnemyConfigEntry(Type type)
        {
            EnemyType = type;
            cleanedName = GetNameForType(type);
            defaultValue = GetDefaultValue(type);
        }

        public void SetConfigEntry(ConfigFile configFile, string sectionName, string description = "")
        {
            configEntry = configFile.Bind(sectionName, cleanedName, defaultValue, "");
        }

        private readonly string GetNameForType(Type type)
        {
            StringBuilder stringBuilder = new StringBuilder(type.Name);
            stringBuilder.Replace("AI", "");
            stringBuilder.Replace("Enemy", "");

            string cleanedName = stringBuilder.ToString();

            stringBuilder = new StringBuilder();
            stringBuilder.Append(cleanedName[0]);

            for (int i = 1; i < cleanedName.Length; i++)
            {
                if (char.IsUpper(cleanedName[i]))
                {
                    stringBuilder.Append(' ');
                }
                stringBuilder.Append(cleanedName[i]);
            }

            return type.Name switch
            {
                nameof(BaboonBirdAI) => "Baboon Hawk",
                nameof(ClaySurgeonAI) => "Barber",
                nameof(FlowermanAI) => "Bracken",
                nameof(SandSpiderAI) => "Bunker Spider",
                nameof(ButlerEnemyAI) => "Butler",
                nameof(ButlerBeesEnemyAI) => "Butler Bees",
                nameof(SpringManAI) => "Coil-Head",
                nameof(RedLocustBees) => "Circuit Bees",
                nameof(SandWormAI) => "Earth Leviathan",
                nameof(MouthDogAI) => "Eyeless Dog",
                nameof(ForestGiantAI) => "Forest Keeper",
                nameof(DressGirlAI) => "Ghost Girl",
                nameof(HoarderBugAI) => "Hoarding Bug",
                nameof(BlobAI) => "Hydrogere",
                nameof(JesterAI) => "Jester",
                nameof(BushWolfEnemy) => "Kidnapper Fox",
                nameof(DoublewingAI) => "Manticoil",
                nameof(MaskedPlayerEnemy) => "Masked",
                nameof(NutcrackerEnemyAI) => "Nutcracker",
                nameof(RadMechAI) => "Old Bird",
                nameof(DocileLocustBeesAI) => "Roaming Locusts",
                nameof(CentipedeAI) => "Snare Flea",
                nameof(PufferAI) => "Spore Lizard",
                nameof(CrawlerAI) => "Thumper",
                nameof(FlowerSnakeEnemy) => "Tulip Snake",
                _ => stringBuilder.ToString()
            };
        }

        private readonly bool GetDefaultValue(Type type)
        {
            switch (type.Name)
            {
                case nameof(DoublewingAI):
                case nameof (DocileLocustBeesAI):
                    return false;
                default:
                    return true;
            }
        }
    }
}
