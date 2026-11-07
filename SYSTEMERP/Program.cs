namespace SYSTEMERP
{
    using System;
    using System.Collections.Generic;
    using MySqlConnector;

    enum Dzial { IT, Ksiegowosc, Kadry, Sprzedaz, Magazyn }
    enum Stanowisko { Praktykant, Pracownik, Specjalista, Kierownik, Dyrektor }
    enum StatusUrlopu { Oczekuje, Zaakceptowany, Odrzucony }

    struct Urlop
    {
        public DateTime Od;
        public DateTime Do;
        public StatusUrlopu Status;
        public string Powod;
    }

    class Wynagrodzenie
    {
        public decimal Brutto;
        public decimal SkladkiSpoleczne { get { return Math.Round(Brutto * 0.1371m, 2); } }
        public decimal PodstawaZdrowotna { get { return Brutto - SkladkiSpoleczne; } }
        public decimal SkladkaZdrowotna { get { return Math.Round(PodstawaZdrowotna * 0.09m, 2); } }
        public decimal Podatek { get { return Math.Max(0, Math.Round(PodstawaZdrowotna * 0.12m - 300m, 2)); } }
        public decimal Netto { get { return Math.Round(Brutto - SkladkiSpoleczne - SkladkaZdrowotna - Podatek, 2); } }

        public Wynagrodzenie(decimal brutto) { Brutto = brutto; }

        public void Wypisz()
        {
            Console.WriteLine("Brutto: " + Brutto.ToString("0.00") + " zł");
            Console.WriteLine("Składki społeczne: " + SkladkiSpoleczne.ToString("0.00") + " zł");
            Console.WriteLine("Składka zdrowotna: " + SkladkaZdrowotna.ToString("0.00") + " zł");
            Console.WriteLine("Podatek: " + Podatek.ToString("0.00") + " zł");
            Console.WriteLine("NETTO DO WYPŁATY: " + Netto.ToString("0.00") + " zł");
        }
    }

    class EwidencjaGodzin
    {
        public int PracownikId;
        public DateTime Data;
        public decimal Godziny;
        public decimal Nadgodziny;
        public string Opis;

        public EwidencjaGodzin(int pracownikId, DateTime data, decimal godziny, decimal nadgodziny, string opis)
        {
            PracownikId = pracownikId;
            Data = data;
            Godziny = godziny;
            Nadgodziny = nadgodziny;
            Opis = opis;
        }
    }

    class Pracownik
    {
        public int Id;
        public string Imie;
        public string Nazwisko;
        public string Email;
        public Dzial Dzial;
        public Stanowisko Stanowisko;
        public decimal Pensja;
        public List<Urlop> Urlopy;

        public Pracownik(int id, string imie, string nazwisko, string email,
            Dzial dzial, Stanowisko stanowisko, decimal pensja)
        {
            Id = id;
            Imie = imie;
            Nazwisko = nazwisko;
            Email = email;
            Dzial = dzial;
            Stanowisko = stanowisko;
            Pensja = pensja;
            Urlopy = new List<Urlop>();
        }

        public void DodajUrlop(DateTime od, DateTime doDate, string powod)
        {
            Urlopy.Add(new Urlop { Od = od, Do = doDate, Powod = powod, Status = StatusUrlopu.Oczekuje });
        }

        public int DniUrlopu()
        {
            int dni = 0;
            foreach (Urlop urlop in Urlopy)
                if (urlop.Status == StatusUrlopu.Zaakceptowany)
                    dni += (urlop.Do - urlop.Od).Days + 1;
            return dni;
        }

        public Wynagrodzenie ObliczWyplate() { return new Wynagrodzenie(Pensja); }

        public void Wypisz()
        {
            Wynagrodzenie wyplata = ObliczWyplate();
            Console.WriteLine($"{Id}. {Imie} {Nazwisko} | {Dzial} | {Stanowisko} | brutto {Pensja:0.00} zł | netto {wyplata.Netto:0.00} zł | {Email}");
        }
    }

    class Firma
    {
        public List<Pracownik> Pracownicy = new List<Pracownik>();
        private readonly string polaczenie;

        public Firma(string polaczenie)
        {
            this.polaczenie = polaczenie;
        }

        public void WczytajZBazy()
        {
            using (MySqlConnection baza = new MySqlConnection(polaczenie))
            {
                baza.Open();
                string sql = "SELECT Id, Imie, Nazwisko, Email, Dzial, Stanowisko, Pensja FROM Pracownicy ORDER BY Nazwisko";
                using (MySqlCommand polecenie = new MySqlCommand(sql, baza))
                using (MySqlDataReader wynik = polecenie.ExecuteReader())
                {
                    while (wynik.Read())
                    {
                        Pracownicy.Add(new Pracownik(
                            wynik.GetInt32("Id"), wynik.GetString("Imie"), wynik.GetString("Nazwisko"),
                            wynik.GetString("Email"), Enum.Parse<Dzial>(wynik.GetString("Dzial")),
                            Enum.Parse<Stanowisko>(wynik.GetString("Stanowisko")), wynik.GetDecimal("Pensja")));
                    }
                }
            }
        }

        public void ZapiszGodziny(EwidencjaGodzin wpis)
        {
            using (MySqlConnection baza = new MySqlConnection(polaczenie))
            {
                baza.Open();
                string sql = "INSERT INTO EwidencjaGodzin (PracownikId, Data, Godziny, Nadgodziny, Opis) VALUES (@id,@data,@godziny,@nadgodziny,@opis)";
                using (MySqlCommand polecenie = new MySqlCommand(sql, baza))
                {
                    polecenie.Parameters.AddWithValue("@id", wpis.PracownikId);
                    polecenie.Parameters.AddWithValue("@data", wpis.Data);
                    polecenie.Parameters.AddWithValue("@godziny", wpis.Godziny);
                    polecenie.Parameters.AddWithValue("@nadgodziny", wpis.Nadgodziny);
                    polecenie.Parameters.AddWithValue("@opis", wpis.Opis);
                    polecenie.ExecuteNonQuery();
                }
            }
        }

        public (decimal godziny, decimal nadgodziny) PobierzGodziny(int pracownikId, int miesiac, int rok)
        {
            using (MySqlConnection baza = new MySqlConnection(polaczenie))
            {
                baza.Open();
                string sql = "SELECT COALESCE(SUM(Godziny),0), COALESCE(SUM(Nadgodziny),0) FROM EwidencjaGodzin WHERE PracownikId=@id AND MONTH(Data)=@miesiac AND YEAR(Data)=@rok";
                using (MySqlCommand polecenie = new MySqlCommand(sql, baza))
                {
                    polecenie.Parameters.AddWithValue("@id", pracownikId);
                    polecenie.Parameters.AddWithValue("@miesiac", miesiac);
                    polecenie.Parameters.AddWithValue("@rok", rok);
                    using (MySqlDataReader wynik = polecenie.ExecuteReader())
                    {
                        wynik.Read();
                        return (wynik.GetDecimal(0), wynik.GetDecimal(1));
                    }
                }
            }
        }

        public Wynagrodzenie ObliczWyplateZaGodziny(Pracownik pracownik, int miesiac, int rok)
        {
            var godziny = PobierzGodziny(pracownik.Id, miesiac, rok);
            decimal stawka = pracownik.Pensja / 168m;
            decimal brutto = godziny.godziny * stawka + godziny.nadgodziny * stawka * 1.5m;
            return new Wynagrodzenie(Math.Round(brutto, 2));
        }

        public void Dodaj(Pracownik pracownik)
        {
            using (MySqlConnection baza = new MySqlConnection(polaczenie))
            {
                baza.Open();
                string sql = "INSERT INTO Pracownicy (Imie, Nazwisko, Email, Dzial, Stanowisko, Pensja) VALUES (@imie,@nazwisko,@email,@dzial,@stanowisko,@pensja)";
                using (MySqlCommand polecenie = new MySqlCommand(sql, baza))
                {
                    polecenie.Parameters.AddWithValue("@imie", pracownik.Imie);
                    polecenie.Parameters.AddWithValue("@nazwisko", pracownik.Nazwisko);
                    polecenie.Parameters.AddWithValue("@email", pracownik.Email);
                    polecenie.Parameters.AddWithValue("@dzial", pracownik.Dzial.ToString());
                    polecenie.Parameters.AddWithValue("@stanowisko", pracownik.Stanowisko.ToString());
                    polecenie.Parameters.AddWithValue("@pensja", pracownik.Pensja);
                    polecenie.ExecuteNonQuery();
                }
            }
            WczytajOdNowa();
        }

        public Pracownik Znajdz(int id)
        {
            foreach (Pracownik pracownik in Pracownicy)
                if (pracownik.Id == id) return pracownik;
            return null;
        }

        public void Usun(int id)
        {
            using (MySqlConnection baza = new MySqlConnection(polaczenie))
            {
                baza.Open();
                using (MySqlCommand polecenie = new MySqlCommand("DELETE FROM Pracownicy WHERE Id=@id", baza))
                {
                    polecenie.Parameters.AddWithValue("@id", id);
                    polecenie.ExecuteNonQuery();
                }
            }
            WczytajOdNowa();
        }

        public Dictionary<Dzial, int> LiczbaPracownikowWDzialach()
        {
            Dictionary<Dzial, int> wynik = new Dictionary<Dzial, int>();
            foreach (Pracownik pracownik in Pracownicy)
                wynik[pracownik.Dzial] = wynik.ContainsKey(pracownik.Dzial) ? wynik[pracownik.Dzial] + 1 : 1;
            return wynik;
        }

        public (decimal najnizsza, decimal najwyzsza) ZakresPensji()
        {
            if (Pracownicy.Count == 0) return (0, 0);
            decimal min = Pracownicy[0].Pensja, max = Pracownicy[0].Pensja;
            foreach (Pracownik pracownik in Pracownicy)
            {
                if (pracownik.Pensja < min) min = pracownik.Pensja;
                if (pracownik.Pensja > max) max = pracownik.Pensja;
            }
            return (min, max);
        }

        public int[] TablicaStanowisk()
        {
            int[] wynik = new int[5];
            foreach (Pracownik pracownik in Pracownicy) wynik[(int)pracownik.Stanowisko]++;
            return wynik;
        }

        private void WczytajOdNowa()
        {
            Pracownicy.Clear();
            WczytajZBazy();
        }
    }

    class Program
    {
        static string polaczenie = "Server=localhost;Port=3306;Database=bazaerp;User ID=root;Password=;SslMode=None;";

        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Firma firma = new Firma(polaczenie);
            try { firma.WczytajZBazy(); }
            catch (Exception blad) { Console.WriteLine("Błąd MySQL: " + blad.Message); Console.ReadKey(); return; }

            bool dziala = true;
            while (dziala)
            {
                Console.Clear();
                Console.WriteLine("=== ERP PRACOWNICY ===");
                Console.WriteLine("1. Lista pracowników");
                Console.WriteLine("2. Dodaj pracownika");
                Console.WriteLine("3. Szukaj pracownika");
                Console.WriteLine("4. Usuń pracownika");
                Console.WriteLine("5. Statystyki");
                Console.WriteLine("6. Kalkulator netto/brutto");
                Console.WriteLine("7. Lista płac");
                Console.WriteLine("8. Zapisz przepracowane godziny");
                Console.WriteLine("9. Wypłata z godzin i nadgodzin");
                Console.WriteLine("0. Koniec");
                Console.Write("Wybierz: ");
                switch (Console.ReadLine())
                {
                    case "1": Lista(firma); break;
                    case "2": Dodaj(firma); break;
                    case "3": Szukaj(firma); break;
                    case "4": Usun(firma); break;
                    case "5": Statystyki(firma); break;
                    case "6": Kalkulator(firma); break;
                    case "7": ListaPlac(firma); break;
                    case "8": ZapiszGodziny(firma); break;
                    case "9": WyplataZGodzin(firma); break;
                    case "0": dziala = false; break;
                    default: Komunikat("Niepoprawna opcja."); break;
                }
            }
        }

        static void Lista(Firma firma)
        {
            Console.Clear();
            foreach (Pracownik pracownik in firma.Pracownicy) pracownik.Wypisz();
            Pauza();
        }

        static void Dodaj(Firma firma)
        {
            Console.Clear();
            Console.Write("Imię: "); string imie = Console.ReadLine();
            Console.Write("Nazwisko: "); string nazwisko = Console.ReadLine();
            Console.Write("Email: "); string email = Console.ReadLine();
            Console.Write("Dział (IT, Ksiegowosc, Kadry, Sprzedaz, Magazyn): ");
            Dzial dzial = Enum.Parse<Dzial>(Console.ReadLine(), true);
            Console.Write("Stanowisko (Praktykant, Pracownik, Specjalista, Kierownik, Dyrektor): ");
            Stanowisko stanowisko = Enum.Parse<Stanowisko>(Console.ReadLine(), true);
            Console.Write("Pensja: "); decimal pensja = decimal.Parse(Console.ReadLine());
            firma.Dodaj(new Pracownik(0, imie, nazwisko, email, dzial, stanowisko, pensja));
            Komunikat("Pracownik został dodany.");
        }

        static void Szukaj(Firma firma)
        {
            Console.Write("Podaj ID: ");
            Pracownik pracownik = firma.Znajdz(int.Parse(Console.ReadLine()));
            if (pracownik == null) Console.WriteLine("Nie znaleziono pracownika."); else pracownik.Wypisz();
            Pauza();
        }

        static void Usun(Firma firma)
        {
            Console.Write("Podaj ID do usunięcia: ");
            firma.Usun(int.Parse(Console.ReadLine()));
            Komunikat("Operacja zakończona.");
        }

        static void Statystyki(Firma firma)
        {
            Console.Clear();
            Console.WriteLine("Pracownicy w działach:");
            foreach (var para in firma.LiczbaPracownikowWDzialach()) Console.WriteLine($"{para.Key}: {para.Value}");
            var zakres = firma.ZakresPensji();
            Console.WriteLine($"Pensja: od {zakres.najnizsza:0.00} do {zakres.najwyzsza:0.00} zł");
            Console.WriteLine("Stanowiska:");
            int[] stanowiska = firma.TablicaStanowisk();
            for (int i = 0; i < stanowiska.Length; i++) Console.WriteLine($"{(Stanowisko)i}: {new string('#', stanowiska[i])}");
            Pauza();
        }

        static void Kalkulator(Firma firma)
        {
            Console.Clear();
            Console.Write("Podaj ID pracownika: ");
            Pracownik pracownik = firma.Znajdz(int.Parse(Console.ReadLine()));
            if (pracownik == null) { Komunikat("Nie znaleziono pracownika."); return; }
            Console.WriteLine("Wynagrodzenie: " + pracownik.Imie + " " + pracownik.Nazwisko);
            pracownik.ObliczWyplate().Wypisz();
            Pauza();
        }

        static void ListaPlac(Firma firma)
        {
            Console.Clear();
            decimal brutto = 0, netto = 0;
            Console.WriteLine("=== LISTA PŁAC ===");
            foreach (Pracownik pracownik in firma.Pracownicy)
            {
                Wynagrodzenie wyplata = pracownik.ObliczWyplate();
                brutto += wyplata.Brutto;
                netto += wyplata.Netto;
                Console.WriteLine(pracownik.Imie + " " + pracownik.Nazwisko + " | brutto: " + wyplata.Brutto.ToString("0.00") + " | netto: " + wyplata.Netto.ToString("0.00") + " zł");
            }
            Console.WriteLine("RAZEM BRUTTO: " + brutto.ToString("0.00") + " zł");
            Console.WriteLine("RAZEM NETTO: " + netto.ToString("0.00") + " zł");
            Pauza();
        }

        static void ZapiszGodziny(Firma firma)
        {
            Console.Clear();
            Console.Write("ID pracownika: ");
            int id = int.Parse(Console.ReadLine());
            if (firma.Znajdz(id) == null) { Komunikat("Nie znaleziono pracownika."); return; }
            Console.Write("Data (rrrr-mm-dd): "); DateTime data = DateTime.Parse(Console.ReadLine());
            Console.Write("Zwykłe godziny: "); decimal godziny = decimal.Parse(Console.ReadLine());
            Console.Write("Nadgodziny: "); decimal nadgodziny = decimal.Parse(Console.ReadLine());
            Console.Write("Opis: "); string opis = Console.ReadLine();
            firma.ZapiszGodziny(new EwidencjaGodzin(id, data, godziny, nadgodziny, opis));
            Komunikat("Godziny zapisano w bazie MySQL.");
        }

        static void WyplataZGodzin(Firma firma)
        {
            Console.Clear();
            Console.Write("ID pracownika: ");
            Pracownik pracownik = firma.Znajdz(int.Parse(Console.ReadLine()));
            if (pracownik == null) { Komunikat("Nie znaleziono pracownika."); return; }
            Console.Write("Miesiąc (1-12): "); int miesiac = int.Parse(Console.ReadLine());
            Console.Write("Rok: "); int rok = int.Parse(Console.ReadLine());
            var godziny = firma.PobierzGodziny(pracownik.Id, miesiac, rok);
            Console.WriteLine("Pracownik: " + pracownik.Imie + " " + pracownik.Nazwisko);
            Console.WriteLine("Zwykłe godziny: " + godziny.godziny + " | nadgodziny: " + godziny.nadgodziny);
            Console.WriteLine("Stawka podstawowa: " + (pracownik.Pensja / 168m).ToString("0.00") + " zł/h");
            Console.WriteLine("Wynagrodzenie z ewidencji:");
            firma.ObliczWyplateZaGodziny(pracownik, miesiac, rok).Wypisz();
            Pauza();
        }

        static void Komunikat(string tekst) { Console.WriteLine(tekst); Pauza(); }
        static void Pauza() { Console.WriteLine("Naciśnij dowolny klawisz..."); Console.ReadKey(); }
    }
}
