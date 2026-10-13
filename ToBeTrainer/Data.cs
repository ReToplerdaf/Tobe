namespace ToBeTrainer;

public enum Verb { Am, Is, Are }
public enum Mode { Positive, Negative, Question }

[Flags]
public enum Kind
{
    Person = 1,   // люди
    Animal = 2,   // животные
    Thing = 4,    // предметы
    Weather = 8,  // погода
    Shop = 16,    // магазин
    It = 32,      // "It is an apple"
}

// Русские формы: 0 = м.р., 1 = ж.р., 2 = ср.р., 3 = мн.ч.
public record Subject(
    string En, string Ru, string RuDat, Verb Verb, Kind Kind,
    int RuForm, bool EnPlural, string EnPronoun);

public record Complement(
    Kind Kinds, string EnSg, string EnPl, string[] Ru,
    bool IsAge = false);

public static class Data
{
    static string[] Same(string s) => new[] { s, s, s, s };
    static string[] Adj(string m, string f, string n, string pl) => new[] { m, f, n, pl };
    static string[] Noun(string m, string f, string pl) => new[] { m, f, m, pl };

    public static readonly Subject[] Subjects =
    {
        new("I", "Я", "Мне", Verb.Am, Kind.Person, 0, false, "I"),
        new("You", "Вы", "Вам", Verb.Are, Kind.Person, 3, false, "you"),
        new("He", "Он", "Ему", Verb.Is, Kind.Person, 0, false, "he"),
        new("She", "Она", "Ей", Verb.Is, Kind.Person, 1, false, "she"),
        new("We", "Мы", "Нам", Verb.Are, Kind.Person, 3, true, "we"),
        new("They", "Они", "Им", Verb.Are, Kind.Person, 3, true, "they"),
        new("My friend", "Мой друг", "Моему другу", Verb.Is, Kind.Person, 0, false, "he"),
        new("My sister", "Моя сестра", "Моей сестре", Verb.Is, Kind.Person, 1, false, "she"),
        new("Dad", "Папа", "Папе", Verb.Is, Kind.Person, 0, false, "he"),
        new("Mum", "Мама", "Маме", Verb.Is, Kind.Person, 1, false, "she"),
        new("John", "Джон", "Джону", Verb.Is, Kind.Person, 0, false, "he"),
        new("Mary", "Мэри", "Мэри", Verb.Is, Kind.Person, 1, false, "she"),
        new("John and Mary", "Джон и Мэри", "Джону и Мэри", Verb.Are, Kind.Person, 3, true, "they"),
        new("My mum and dad", "Мои мама и папа", "Моим маме и папе", Verb.Are, Kind.Person, 3, true, "they"),
        new("The children", "Дети", "Детям", Verb.Are, Kind.Person, 3, true, "they"),
        new("Tom and I", "Мы с Томом", "Нам с Томом", Verb.Are, Kind.Person, 3, true, "we"),
        new("The cat", "Кошка", "Кошке", Verb.Is, Kind.Animal, 1, false, "it"),
        new("The dog", "Собака", "Собаке", Verb.Is, Kind.Animal, 1, false, "it"),
        new("The dogs", "Собаки", "Собакам", Verb.Are, Kind.Animal, 3, true, "they"),
        new("The book", "Книга", "", Verb.Is, Kind.Thing, 1, false, "it"),
        new("This TV", "Этот телевизор", "", Verb.Is, Kind.Thing, 0, false, "it"),
        new("The car", "Машина", "", Verb.Is, Kind.Thing, 1, false, "it"),
        new("These phones", "Эти телефоны", "", Verb.Are, Kind.Thing, 3, true, "they"),
        new("The weather", "Погода", "", Verb.Is, Kind.Weather, 1, false, "it"),
        new("This shop", "Этот магазин", "", Verb.Is, Kind.Shop, 0, false, "it"),
        new("The shops", "Магазины", "", Verb.Are, Kind.Shop, 3, true, "they"),
        new("It", "Это", "", Verb.Is, Kind.It, 2, false, "it"),
    };

    const Kind PA = Kind.Person | Kind.Animal;
    const Kind PAT = Kind.Person | Kind.Animal | Kind.Thing;
    const Kind AT = Kind.Animal | Kind.Thing;

    public static readonly Complement[] Complements =
    {
        // состояния
        new(PA, "hungry", "hungry", Adj("голоден", "голодна", "голодно", "голодны")),
        new(PA, "happy", "happy", Adj("счастлив", "счастлива", "счастливо", "счастливы")),
        new(PA, "tired", "tired", Adj("устал", "устала", "устало", "устали")),
        new(Kind.Person, "sad", "sad", Adj("грустный", "грустная", "грустное", "грустные")),
        new(Kind.Person, "busy", "busy", Adj("занят", "занята", "занято", "заняты")),
        new(PA, "ill", "ill", Adj("болен", "больна", "больно", "больны")),

        // места
        new(PA, "at home", "at home", Same("дома")),
        new(Kind.Person, "in the gym", "in the gym", Same("в спортзале")),
        new(PA, "in the park", "in the park", Same("в парке")),
        new(PA, "in the garden", "in the garden", Same("в саду")),
        new(Kind.Person, "at school", "at school", Same("в школе")),
        new(Kind.Person, "at work", "at work", Same("на работе")),
        new(Kind.Person, "in London", "in London", Same("в Лондоне")),
        new(Kind.Person, "from England", "from England", Same("из Англии")),
        new(Kind.Person, "from Russia", "from Russia", Same("из России")),
        new(AT, "under the table", "under the table", Same("под столом")),
        new(PAT, "in the kitchen", "in the kitchen", Same("на кухне")),
        new(AT, "on the sofa", "on the sofa", Same("на диване")),

        // профессии / кто это
        new(Kind.Person, "a doctor", "doctors", Noun("врач", "врач", "врачи")),
        new(Kind.Person, "a teacher", "teachers", Noun("учитель", "учительница", "учителя")),
        new(Kind.Person, "a student", "students", Noun("студент", "студентка", "студенты")),
        new(Kind.Person, "a singer", "singers", Noun("певец", "певица", "певцы")),
        new(Kind.Person, "a pupil", "pupils", Noun("ученик", "ученица", "ученики")),
        new(Kind.Person, "a good friend", "good friends", Noun("хороший друг", "хорошая подруга", "хорошие друзья")),
        new(Kind.Person, "a pilot", "pilots", Noun("пилот", "пилот", "пилоты")),

        // возраст
        new(Kind.Person, "{n} years old", "{n} years old", Same(""), IsAge: true),

        // предметы и животные
        new(AT, "big", "big", Adj("большой", "большая", "большое", "большие")),
        new(AT, "small", "small", Adj("маленький", "маленькая", "маленькое", "маленькие")),
        new(Kind.Thing, "new", "new", Adj("новый", "новая", "новое", "новые")),
        new(Kind.Thing, "old", "old", Adj("старый", "старая", "старое", "старые")),
        new(Kind.Thing, "expensive", "expensive", Adj("дорогой", "дорогая", "дорогое", "дорогие")),
        new(Kind.Thing, "interesting", "interesting", Adj("интересный", "интересная", "интересное", "интересные")),
        new(Kind.Animal, "black", "black", Adj("чёрный", "чёрная", "чёрное", "чёрные")),

        // погода
        new(Kind.Weather, "nice today", "nice today", Same("хорошая сегодня")),
        new(Kind.Weather, "cold today", "cold today", Same("холодная сегодня")),
        new(Kind.Weather, "sunny today", "sunny today", Same("солнечная сегодня")),
        new(Kind.Weather, "bad today", "bad today", Same("плохая сегодня")),

        // магазин
        new(Kind.Shop, "open today", "open today", Adj("открыт сегодня", "открыта сегодня", "открыто сегодня", "открыты сегодня")),
        new(Kind.Shop, "closed now", "closed now", Adj("закрыт сейчас", "закрыта сейчас", "закрыто сейчас", "закрыты сейчас")),

        // It is ...
        new(Kind.It, "an apple", "", Same("яблоко")),
        new(Kind.It, "a book", "", Same("книга")),
        new(Kind.It, "a pen", "", Same("ручка")),
        new(Kind.It, "a ball", "", Same("мяч")),
        new(Kind.It, "an orange", "", Same("апельсин")),
        new(Kind.It, "my bag", "", Same("моя сумка")),
    };
}
