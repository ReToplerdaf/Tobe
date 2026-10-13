using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ToBeTrainer;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var stats = Stats.Load();
var trainer = new Trainer(stats);

Ui.Title();
Mode[] modes = Ui.ChooseModes();
Ui.Help();

int asked = 0, answered = 0, correct = 0, streak = 0;

while (true)
{
    var task = trainer.Next(modes);
    asked++;
    Ui.ShowTask(task, asked);

    string? input;
    while (true)
    {
        Console.Write("  > ");
        input = Console.ReadLine();
        if (input == null) { Finish(); return; }
        string cmd = input.Trim().ToLowerInvariant();

        if (cmd is "q" or "exit" or "quit" or "выход") { Finish(); return; }
        if (cmd is "?" or "h" or "help" or "подсказка")
        {
            Ui.Gray($"  Перевод: {task.Ru}");
            continue;
        }
        if (cmd is "stats" or "s" or "статистика") { Ui.ShowStats(stats); continue; }
        if (cmd is "mode" or "m" or "режим") { modes = Ui.ChooseModes(); asked--; goto nextTask; }
        if (cmd.Length == 0) continue;
        break;
    }

    bool ok = task.Check(input);
    trainer.Record(task, ok);
    stats.Save();

    answered++;
    if (ok) { correct++; streak++; } else streak = 0;
    Ui.ShowResult(task, ok, input, correct, answered, streak);

nextTask:;
}

void Finish()
{
    stats.Save();
    Console.WriteLine();
    if (answered > 0)
        Ui.Gray($"  Итог сессии: {correct} из {answered} верно. Прогресс сохранён. Пока!");
    else
        Ui.Gray("  Пока!");
}

namespace ToBeTrainer
{
    public class TaskItem
    {
        public required Subject Subject { get; init; }
        public required Complement Complement { get; init; }
        public required Mode Mode { get; init; }
        public int Age { get; init; }

        public Verb Verb => Subject.Verb;
        public string StatKey => $"{Mode}:{Verb}";

        string ComplementEn
        {
            get
            {
                var c = Complement;
                string en = Subject.EnPlural ? c.EnPl : c.EnSg;
                return en.Replace("{n}", Age.ToString());
            }
        }

        // Варианты правильного ответа (первый — «основной»)
        public string[] Answers => (Mode, Verb) switch
        {
            (Mode.Negative, Verb.Am) => new[] { "am not", "'m not" },
            (Mode.Negative, Verb.Is) => new[] { "isn't", "is not", "'s not" },
            (Mode.Negative, Verb.Are) => new[] { "aren't", "are not", "'re not" },
            (_, Verb.Am) => new[] { "am" },
            (_, Verb.Is) => new[] { "is" },
            _ => new[] { "are" },
        };

        string SubjForSentence => Mode == Mode.Question && Subject.En != "I"
            ? char.ToLower(Subject.En[0]) + Subject.En[1..]
            : Subject.En;

        // "Dad" / "Mum" / "John" и т.п. с большой буквы и внутри вопроса
        string SubjInQuestion => Subject.En is "Dad" or "Mum" or "John" or "Mary" or "John and Mary" or "Tom and I"
            ? Subject.En : SubjForSentence;

        public string Prompt => Mode == Mode.Question
            ? $"_____ {SubjInQuestion} {ComplementEn}?"
            : $"{Subject.En} _____ {ComplementEn}.";

        public string Full(string answer) => Mode == Mode.Question
            ? $"{Cap(answer)} {SubjInQuestion} {ComplementEn}?"
            : answer.StartsWith('\'')
                ? $"{Subject.En}{answer} {ComplementEn}."
                : $"{Subject.En} {answer} {ComplementEn}.";

        public string Ru
        {
            get
            {
                string ne = Mode == Mode.Negative ? "не " : "";
                string end = Mode == Mode.Question ? "?" : ".";
                if (Complement.IsAge)
                    return $"{Subject.RuDat} {ne}{Age} лет{end}";
                return $"{Subject.Ru} {ne}{Complement.Ru[Subject.RuForm]}{end}";
            }
        }

        public bool Check(string input)
        {
            string a = Norm(input);
            foreach (var ans in Answers)
            {
                if (a == Norm(ans)) return true;
                if (a == Norm(Full(ans))) return true; // ввели всё предложение
            }
            return false;
        }

        public static string Norm(string s)
        {
            s = s.ToLowerInvariant().Replace('’', '\'').Replace('`', '\'').Replace('‘', '\'');
            s = Regex.Replace(s, @"[.?!,]", " ");
            s = Regex.Replace(s, @"\s+", " ").Trim();
            s = Regex.Replace(s, @"\s+'", "'"); // "he 's not" -> "he's not"
            s = Regex.Replace(s, @"\b(isnt|arent)\b", m => m.Value[..^1] + "'t"); // без апострофа тоже засчитываем
            return s;
        }

        static string Cap(string s) => char.ToUpper(s[0]) + s[1..];

        public string RuleHint()
        {
            string form = Verb switch { Verb.Am => "am", Verb.Is => "is", _ => "are" };
            string neg = Verb switch { Verb.Am => "am not", Verb.Is => "isn't / is not", _ => "aren't / are not" };
            string what = Mode == Mode.Negative ? neg : form;

            string why = Subject.En switch
            {
                "I" => "I → всегда am",
                "You" => "You → всегда are (даже про одного человека)",
                _ when Subject.En == Subject.EnPronoun || Subject.En.ToLower() == Subject.EnPronoun
                    => $"{Subject.En} → {form}",
                _ => $"«{Subject.En}» = {Subject.EnPronoun} → {form}",
            };

            string extra = Mode switch
            {
                Mode.Negative => $"   Отрицание: {what}",
                Mode.Question => $"   В вопросе глагол встаёт на первое место: {Cap(form)} {SubjInQuestion} ...?",
                _ => "",
            };
            return "  Правило: " + why + (extra.Length > 0 ? "\n" + "  " + extra.Trim() : "");
        }
    }

    public class Stat
    {
        public int Attempts { get; set; }
        public int Errors { get; set; }
        public double Weakness { get; set; }
    }

    public class Stats
    {
        public Dictionary<string, Stat> Items { get; set; } = new();

        static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ToBeTrainer", "stats.json");

        public Stat Get(string key)
        {
            if (!Items.TryGetValue(key, out var s)) Items[key] = s = new Stat();
            return s;
        }

        public double Weight(string key) =>
            Items.TryGetValue(key, out var s) ? 1 + 3 * s.Weakness : 1;

        public void Record(string key, bool ok)
        {
            var s = Get(key);
            s.Attempts++;
            if (ok) s.Weakness *= 0.6;
            else { s.Errors++; s.Weakness = Math.Min(5, s.Weakness + 1); }
        }

        public static Stats Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonSerializer.Deserialize<Stats>(File.ReadAllText(FilePath)) ?? new Stats();
            }
            catch { }
            return new Stats();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        public void Reset()
        {
            Items.Clear();
            Save();
        }
    }

    public class Trainer(Stats stats)
    {
        readonly Random rnd = new();
        readonly Queue<(int due, TaskItem task)> review = new();
        readonly Queue<string> recent = new();
        int turn;

        public TaskItem Next(Mode[] modes)
        {
            turn++;

            // Повтор ошибки через несколько заданий
            if (review.Count > 0 && review.Peek().due <= turn && modes.Contains(review.Peek().task.Mode))
            {
                var t = review.Dequeue().task;
                return new TaskItem { Subject = t.Subject, Complement = t.Complement, Mode = t.Mode, Age = t.Age };
            }

            for (int attempt = 0; attempt < 50; attempt++)
            {
                // 1) режим + форма глагола — по слабым местам
                var combos = modes.SelectMany(m => new[] { Verb.Am, Verb.Is, Verb.Are }.Select(v => (m, v))).ToArray();
                var (mode, verb) = Pick(combos, c => stats.Weight($"{c.m}:{c.v}") * (c.v == Verb.Am ? 0.6 : 1));

                // 2) подлежащее этой формы — тоже с учётом ошибок
                var subjects = Data.Subjects.Where(s => s.Verb == verb).ToArray();
                var subj = Pick(subjects, s => stats.Weight("subj:" + s.En));

                // 3) подходящее дополнение
                var comps = Data.Complements
                    .Where(c => (c.Kinds & subj.Kind) != 0)
                    .Where(c => !(c.IsAge && subj.Kind != Kind.Person))
                    .ToArray();
                if (comps.Length == 0) continue;
                var comp = comps[rnd.Next(comps.Length)];

                var task = new TaskItem { Subject = subj, Complement = comp, Mode = mode, Age = rnd.Next(7, 16) };
                string sig = subj.En + "|" + comp.EnSg;
                if (recent.Contains(sig)) continue;

                recent.Enqueue(sig);
                if (recent.Count > 12) recent.Dequeue();
                return task;
            }
            // запасной вариант
            return new TaskItem { Subject = Data.Subjects[0], Complement = Data.Complements[0], Mode = modes[0] };
        }

        public void Record(TaskItem task, bool ok)
        {
            stats.Record(task.StatKey, ok);
            stats.Record("subj:" + task.Subject.En, ok);
            if (!ok) review.Enqueue((turn + rnd.Next(3, 6), task));
        }

        T Pick<T>(T[] items, Func<T, double> weight)
        {
            double total = items.Sum(weight);
            double r = rnd.NextDouble() * total;
            foreach (var it in items)
            {
                r -= weight(it);
                if (r <= 0) return it;
            }
            return items[^1];
        }
    }

    public static class Ui
    {
        public static void Title()
        {
            try { Console.Clear(); } catch { }
            Color(ConsoleColor.Cyan, "\n  ═════════════════════════════════");
            Color(ConsoleColor.Cyan, "     Тренажёр глагола  TO BE");
            Color(ConsoleColor.Cyan, "        am  •  is  •  are");
            Color(ConsoleColor.Cyan, "  ═════════════════════════════════\n");
        }

        public static Mode[] ChooseModes()
        {
            Console.WriteLine("  Выбери режим:");
            Console.WriteLine("    1 — всё вперемешку");
            Console.WriteLine("    2 — am / is / are            (He ___ in the gym.)");
            Console.WriteLine("    3 — отрицание                (He isn't in the gym.)");
            Console.WriteLine("    4 — вопросы                  (___ he in the gym?)");
            Console.WriteLine("    0 — сбросить статистику ошибок");
            while (true)
            {
                Console.Write("  > ");
                string? s = Console.ReadLine()?.Trim();
                switch (s)
                {
                    case null: Environment.Exit(0); break;
                    case "" or "1": return new[] { Mode.Positive, Mode.Negative, Mode.Question };
                    case "2": return new[] { Mode.Positive };
                    case "3": return new[] { Mode.Negative };
                    case "4": return new[] { Mode.Question };
                    case "0":
                        Stats.Load().Reset();
                        Environment.Exit(0);
                        break;
                }
            }
        }

        public static void Help()
        {
            Gray("\n  Впиши пропущенное слово и нажми Enter. Можно ввести и всё предложение.");
            Gray("  Команды:  ?  — перевод-подсказка   stats — твои слабые места");
            Gray("            mode — сменить режим     q — выход\n");
        }

        public static void ShowTask(TaskItem t, int n)
        {
            string label = t.Mode switch
            {
                Mode.Positive => "(+) утверждение",
                Mode.Negative => "(−) отрицание",
                _ => "(?) вопрос",
            };
            Console.WriteLine();
            Color(ConsoleColor.DarkGray, $"  ── #{n}  {label} ─────────────────────");
            Color(ConsoleColor.White, "  " + t.Prompt);
        }

        public static void ShowResult(TaskItem t, bool ok, string input, int correct, int asked, int streak)
        {
            if (ok)
            {
                Color(ConsoleColor.Green, "  ✔ Правильно!" + (streak >= 3 ? $"  🔥 серия {streak}" : ""));
                Color(ConsoleColor.Green, "    " + t.Full(t.Answers[0]));
            }
            else
            {
                Color(ConsoleColor.Red, $"  ✘ Неправильно. Ты написал(а): «{input.Trim()}»");
                string alts = t.Answers.Length > 1 ? $"  (или: {string.Join(", ", t.Answers.Skip(1).Where(a => !a.StartsWith('\'')))})" : "";
                Color(ConsoleColor.Yellow, $"    Верно: {t.Answers[0]}{alts}");
                Color(ConsoleColor.Yellow, "    " + t.Full(t.Answers[0]));
                Color(ConsoleColor.DarkYellow, t.RuleHint());
                Color(ConsoleColor.DarkGray, "  (это предложение попадётся ещё раз чуть позже)");
            }
            Color(ConsoleColor.Cyan, $"  🇷🇺 {t.Ru}");
            Color(ConsoleColor.DarkGray, $"  Счёт: {correct}/{asked}");
        }

        public static void ShowStats(Stats stats)
        {
            Console.WriteLine();
            Color(ConsoleColor.Cyan, "  Статистика (за всё время)");
            var forms = stats.Items.Where(kv => !kv.Key.StartsWith("subj:")).OrderByDescending(kv => kv.Value.Weakness).ToList();
            if (forms.Count == 0) { Gray("  Пока нет данных."); return; }

            foreach (var (key, s) in forms)
            {
                var parts = key.Split(':');
                string mode = parts[0] switch { "Positive" => "утверждение", "Negative" => "отрицание", _ => "вопрос" };
                string verb = parts[1].ToLower();
                int pct = s.Attempts == 0 ? 0 : 100 * (s.Attempts - s.Errors) / s.Attempts;
                var col = s.Weakness > 1 ? ConsoleColor.Red : s.Weakness > 0.3 ? ConsoleColor.Yellow : ConsoleColor.Green;
                Color(col, $"    {verb,-4} {mode,-12} {pct,3}% верно   ({s.Attempts - s.Errors}/{s.Attempts})");
            }

            var weakSubj = stats.Items.Where(kv => kv.Key.StartsWith("subj:") && kv.Value.Weakness > 0.5)
                .OrderByDescending(kv => kv.Value.Weakness).Take(5).Select(kv => kv.Key[5..]).ToList();
            if (weakSubj.Count > 0)
                Color(ConsoleColor.Yellow, "  Чаще ошибаешься с: " + string.Join(", ", weakSubj));
            Gray("  Такие задания будут попадаться чаще.\n");
        }

        public static void Gray(string s) => Color(ConsoleColor.DarkGray, s);

        public static void Color(ConsoleColor c, string s)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = c;
            Console.WriteLine(s);
            Console.ForegroundColor = old;
        }
    }
}
