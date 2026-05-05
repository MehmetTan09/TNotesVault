using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace TNotesUltimateEdition
{
    #region 1. GÜVENLİ PENCERE YÖNETİMİ & UX EFEKTLERİ
    public static class WindowManager
    {
        public static void OptimizeConsole()
        {
            try { Console.Title = "T NOTES VAULT | SECURE KERNEL 2026"; } catch { }
            try { Console.OutputEncoding = Encoding.UTF8; } catch { }
            try { Console.CursorVisible = false; } catch { }

            try
            {
                // Modern Windows Terminal ve CMD uyumluluğu için kontroller
                int maxW = Console.LargestWindowWidth;
                int maxH = Console.LargestWindowHeight;

                if (maxW > 0 && maxH > 0)
                {
                    int w = Math.Min(120, maxW - 2);
                    int h = Math.Min(35, maxH - 2);

                    if (w > 20 && h > 10)
                    {
                        // Önce buffer sonra window ayarlamak Windows hatasını önler
                        Console.SetBufferSize(w, h);
                        Console.SetWindowSize(w, h);
                    }
                }
            }
            catch { /* Bazı terminaller boyutlandırmaya izin vermez, sessizce geçilir */ }
        }

        public static void BootAnim()
        {
            try { Console.Clear(); } catch { }
            string[] frames = { "[/]", "[-]", "[\\]", "[|]" };
            Console.ForegroundColor = ConsoleColor.Cyan;
            for (int i = 0; i < 15; i++)
            {
                try
                {
                    Console.SetCursorPosition(2, 1);
                    Console.Write($"CORE INITIALIZATION... {frames[i % 4]} {i * 7}%");
                }
                catch { }
                Thread.Sleep(60);
            }
            try { Console.Clear(); } catch { }
        }
    }
    #endregion

    #region 2. VERİ MODELLERİ
    [Serializable]
    public class NoteEntry
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string EncryptedContent { get; set; }
        public DateTime CreatedAt { get; set; }

        public NoteEntry()
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
            CreatedAt = DateTime.Now;
        }
    }

    [Serializable]
    public class UserAccount
    {
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public int LanguageId { get; set; } = 2;
        public int ThemeId { get; set; } = 1;
    }

    [Serializable]
    public class AppConfig
    {
        public List<UserAccount> Users { get; set; } = new List<UserAccount>();
        public int SystemLanguageId { get; set; } = 2;
    }
    #endregion

    #region 3. GÜVENLİK MOTORU (AES-256)
    public static class SecurityEngine
    {
        private static readonly string KeyString = "TNOTES_VAULT_SECURE_TAN_2026!";
        private static byte[] ValidKeyBytes;

        static SecurityEngine()
        {
            ValidKeyBytes = new byte[32];
            byte[] stringBytes = Encoding.UTF8.GetBytes(KeyString);
            Array.Copy(stringBytes, ValidKeyBytes, Math.Min(stringBytes.Length, 32));
        }

        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes) builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            byte[] iv = new byte[16];
            using (Aes aes = Aes.Create())
            {
                aes.Key = ValidKeyBytes;
                aes.IV = iv;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs)) { sw.Write(plainText); }
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;
            try
            {
                byte[] iv = new byte[16];
                byte[] buffer = Convert.FromBase64String(cipherText);
                using (Aes aes = Aes.Create())
                {
                    aes.Key = ValidKeyBytes;
                    aes.IV = iv;
                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    using (MemoryStream ms = new MemoryStream(buffer))
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader sr = new StreamReader(cs)) { return sr.ReadToEnd(); }
                        }
                    }
                }
            }
            catch { return "[DECRYPTION_ERROR]"; }
        }
    }
    #endregion

    #region 4. DİL VE TEMA MOTORU (TAM MATRİS)
    public static class LangEngine
    {
        public static int CurrentLang { get; set; } = 2;

        public static readonly string[] LanguageNames = {
            "Türkçe", "English", "中文", "हिन्दी", "Español", "Français", "العربية", "বাংলা", "Русский", "Português",
            "اردو", "Indonesia", "Deutsch", "日本語", "मराठी", "తెలుగు", "Italiano", "தமிழ்", "Tiếng Việt", "한국어",
            "Polski", "Nederlands", "Svenska", "Ελληνικά", "Čeština"
        };

        private static readonly string[] CultureCodes = {
            "tr-TR", "en-US", "zh-CN", "hi-IN", "es-ES", "fr-FR", "ar-SA", "bn-IN", "ru-RU", "pt-PT",
            "ur-PK", "id-ID", "de-DE", "ja-JP", "mr-IN", "te-IN", "it-IT", "ta-IN", "vi-VN", "ko-KR",
            "pl-PL", "nl-NL", "sv-SE", "el-GR", "cs-CZ"
        };

        public static CultureInfo GetCulture()
        {
            try
            {
                int idx = Math.Max(0, Math.Min(CurrentLang - 1, CultureCodes.Length - 1));
                return CultureInfo.CreateSpecificCulture(CultureCodes[idx]);
            }
            catch { return CultureInfo.InvariantCulture; }
        }

        public static void ChangeLanguage(int langId)
        {
            CurrentLang = langId;
            var culture = GetCulture();
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        private static readonly Dictionary<string, string[]> dict = new Dictionary<string, string[]>
        {
            { "App_Name", new[] { "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT", "T NOTES VAULT" } },
            { "App_Motto", new[] { "Düşünceleriniz, Sonsuza Dek Güvende", "Your Thoughts, Secured Forever", "您的思想，永久保护", "आपके विचार, हमेशा के लिए सुरक्षित", "Tus pensamientos, asegurados para siempre", "Vos pensées, sécurisées pour toujours", "أفكارك، مؤمنة للأبد", "আপনার চিন্তা, চিরতরে সুরক্ষিত", "Ваши мысли под надежной защитой", "Seus pensamentos, protegidos para sempre", "آپ کے خیالات، ہمیشہ کے لیے محفوظ", "Pikiran Anda, Terlindungi Selamanya", "Ihre Gedanken, für immer gesichert", "あなたの考え、永遠に守られる", "तुमचे विचार, कायमचे सुरक्षित", "నీ ఆలోచనలు, శాశ్వతంగా bhadram", "I tuoi pensieri, protetti per sempre", "உங்கள் எண்ணங்கள், என்றும் பாதுகாப்பானவை", "Suy nghĩ của bạn, được bảo mật mãi mãi", "당신의 생각, 영원히 보호됩니다", "Twoje myśli, zabezpieczone na zawsze", "Uw gedachten, voor altijd beveiligd", "Dina tankar, säkrade för evigt", "Οι σκέψεις σας, ασφαλείς για πάντα", "Vaše myšlenky, navždy v bezpečí" } },
            { "Auth_Title", new[] { "SİSTEM BAŞLATICI", "SYSTEM BOOT", "系统启动", "सिस्टम बूट", "INICIO DEL SISTEMA", "DÉMARRAGE SYSTÈME", "إقلاع النظام", "সিস্টেম বুট", "ЗАГРУЗКА СИСТЕМЫ", "INICIALIZAÇÃO", "سستم بوٹ", "BOOT SISTEM", "SYSTEMSTART", "システム起動", "सिस्टम बूट", "సిస్టమ్ బూట్", "AVVIO SISTEMA", "கணினி துவக்கம்", "KHỞI ĐỘNG HỆ THỐNG", "시스템 부팅", "ROZRUCH SYSTEMU", "SYSTEEM BOOT", "SYSTEMSTART", "ΕΚΚΙΝΗΣΗ ΣΥΣΤΗΜΑΤΟΣ", "SPUŠTĚNİ SYSTÉMU" } },
            { "Auth_UserLabel", new[] { "KULLANICI", "USER", "用户", "उपयोगकर्ता", "USUARIO", "UTILISATEUR", "المستخدم", "ব্যবহারকারী", "ПОЛЬЗОВАТЕЛЬ", "USUÁRIO", "صارف", "PENGGUNA", "BENUTZER", "ユーザー", "वाperकर्ता", "వినియోగదారు", "UTENTE", "பயனர்", "NGƯỜI DÙNG", "사용자", "UŻYTKOWNIK", "GEBRUIKER", "ANVÄNDARE", "ΧΡΗΣΤΗΣ", "UŽIVATEL" } },
            { "Auth_LoginBtn", new[] { "Giriş Yap", "Login", "登录", "लॉग इन", "Iniciar Sesión", "Connexion", "تسجيل الدخول", "লগইন", "Войти", "Entrar", "لاگ ان", "Masuk", "Anmelden", "ログイン", "लॉगिन", "లాగిన్", "Accedi", "உள்நுழைக", "Đăng nhập", "로그인", "Zaloguj", "Inloggen", "Logga in", "Σύνδεση", "Přihlásit se" } },
            { "Auth_RegBtn", new[] { "Kayıt Ol", "Register", "注册", "रजिस्टर", "Registrarse", "S'inscrire", "تسجيل", "নিবন্ধন", "Регистрация", "Registrar", "رجسٹر", "Daftar", "Registrieren", "登録", "नोंदणी", "నమోదు", "Registrati", "பதிவு", "Đăng ký", "등록", "Zarejestruj", "Registreren", "Registrera", "Εγγραφή", "Registrovat" } },
            { "Auth_User", new[] { "Kullanıcı Adı:", "Username:", "用户名:", "उपयोगकर्ता:", "Usuario:", "Nom d'utilisateur:", "اسم المستخدم:", "ব্যবহারকারী:", "Имя пользователя:", "Usuário:", "صارف نام:", "Nama Pengguna:", "Benutzername:", "ユーザー名:", "वापरकर्तानाव:", "వినియోగదారు పేరు:", "Nome Utente:", "பயனர்பெயர்:", "Tên đ.nhập:", "사용자 이름:", "Nazwa użytk.:", "Gebruikersnaam:", "Användarnamn:", "Όνομα Χρήστη:", "Uživatelské jméno:" } },
            { "Auth_Pass", new[] { "Şifre:", "Password:", "密码:", "पासवर्ड:", "Contraseña:", "Mot de Passe:", "كلمة المرور:", "পাসवर्ड:", "Пароль:", "Senha:", "پاس ورڈ:", "Sandi:", "Passwort:", "パスワード:", "パスワード:", "పాస్వర్డ్:", "Password:", "கடவுச்சொல்:", "Mật khẩu:", "비밀번호:", "Hasło:", "Wachtwoord:", "Lösenord:", "Κωδικός:", "Heslo:" } },
            { "Auth_NoUser", new[] { "KULLANICI BULUNAMADI!", "USER NOT FOUND!", "未找到用户!", "उपयोगकर्ता नहीं मिला!", "¡USUARIO NO ENCONTRADO!", "UTILISATEUR INTROUVABLE!", "المستخدم غير موجود!", "ব্যবহারকারী পাওয়া যায়নি!", "ПОЛЬЗОВАТЕЛЬ НЕ НАЙДЕН!", "USUÁRIO NÃO ENCONTRADO!", "صارف نہیں ملا!", "PENGGUNA TIDAK DITEMUKAN!", "BENUTZER NICHT GEFUNDEN!", "ユーザーが見つかりません!", "वाperकर्ता आढळला नाही!", "వినియోగదారు కనుగొనబడలేదు!", "UTENTE NON TROVATO!", "பயனர் காணப்படவில்லை!", "KHÔNG TÌM THẤY NGƯỜI DÙNG!", "사용자를 찾을 수 없음!", "NIE ZNALEZIONO UŻYTKOWNIKA!", "GEBRUIKER NIET GEVONDEN!", "ANVÄNDAREN HITTADES INTE!", "Ο ΧΡΗΣΤΗΣ ΔΕΝ ΒΡΕΘΗΚΕ!", "UŽIVATEL NENALEZEN!" } },
            { "Auth_Exists", new[] { "BU KULLANICI ZATEN VAR!", "USER ALREADY EXISTS!", "用户已存在!", "उपयोगकर्ता पहले से मौजूद है!", "¡EL USUARIO YA EXISTE!", "L'UTILISATEUR EXISTE DÉJÀ!", "المستخدم موجود بالفعل!", "ব্যবহারকারী ইতিমধ্যে বিদ্যমান!", "ПОЛЬЗОВАТЕЛЬ УЖЕ СУЩЕСТВУЕТ!", "USUÁRIO JÁ EXISTE!", "صارف پہلے ہی मौजूद है!", "PENGGUNA SUDAH ADA!", "BENUTZER EXISTIERT BEREITS!", "ユーザーは既に存在します!", "वापरकर्ता आधीपासून अस्तित्वात आहे!", "విنيوگదారు ఇప్పటికే ఉన్నారు!", "L'UTENTE ESISTE GIÀ!", "பயனர் ஏற்கனவே உள்ளார்!", "NGƯỜI DÙNG ĐÃ TỒN TẠI!", "사용자가 이미 존재합니다!", "UŻYTKOWNIK JUŻ ISTNIEJE!", "GEBRUIKER BESTAAT AL!", "ANVÄNDAREN FINNS REDAN!", "Ο ΧΡΗΣΤΗΣ ΥΠΑΡΧΕΙ ΗΔΗ!", "UŽIVATEL JIŻ EXISTUJE!" } },
            { "Auth_RegOK", new[] { "KAYIT BAŞARILI!", "REGISTRATION SUCCESS!", "注册成功!", "पंजीकरण सफल!", "¡REGISTRO EXITOSO!", "INSCRIPTION RÉUSSIE!", "نجاح التسجيل!", "নিবন্ধন सफल!", "РЕГИСТРАЦИЯ УСПЕШНА!", "REGISTRO BEM-SUCEDIDO!", "رجسٹریشن کامیاب!", "PENDAFTARAN BERHASIL!", "REGISTRIERUNG ERFOLGREICH!", "登録成功!", "नोंदणी यशस्वी!", "నమోదు విజయవంతమైంది!", "REGISTRAZIONE AVVENUTA!", "பதிவு வெற்றி!", "ĐĂNG KÝ THÀNH CÔNG!", "등록 성공!", "REJESTRACJA ZAKOŃCZONA!", "REGISTRATIE VOLTOOID!", "REGISTRERING LYCKADES!", "ΕΠΙΤΥΧΗΣ ΕΓΓΡΑΦΗ!", "REGISTRACE ÚSPĚŠNÁ!" } },
            { "Auth_Error", new[] { "HATALI ŞİFRE!", "INVALID PASSWORD!", "密码无效!", "अमान्य पासवर्ड!", "CONTRASEÑA INVÁLIDA!", "MOT DE PASSE INVALIDE!", "كلمة مرور خاطئة!", "ভুল पासवर्ड!", "НЕВЕРНЫЙ ПАРОЛЬ!", "SENHA INVÁLIDA!", "غلط پاس ورڈ!", "SANDI SALAH!", "FALSCHES PASSWORT!", "無効なパスワード!", "अवैध पासवर्ड!", "చెల్లని పాస్वर्ड!", "PASSWORD NON VALIDA!", "தவறான கடவுச்சொல்!", "SAI MẬT KHẨU!", "잘못된 비밀번호!", "BŁĘDNE HASŁO!", "ONGELDIG WACHTWOORD!", "OGILTIGT LÖSENORD!", "ΛΑΘΟΣ ΚΩΔΙΚΟΣ!", "NEPLATNÉ HESLO!" } },
            { "Copyright", new[] { "© 2026 TAN TARAFINDAN KODLANDI | TÜM HAKLARI SAKLIDIR", "© 2026 CODED BY TAN | ALL RIGHTS RESERVED", "© 2026 由 TAN 编写 | 版权所有", "© 2026 TAN द्वारा कोड किया गया | सर्वाधिकार सुरक्षित", "© 2026 CODIFICADO POR TAN | TODOS LOS DERECHOS RESERVADOS", "© 2026 CODÉ PAR TAN | TOUS DROITS RÉSERVÉS", "© 2026 مبرمج بواسطة TAN | جميع الحقوق محفوظة", "© 2026 TAN द्वारा कोडকৃত | সর্বস্বত্ব সংরক্ষিত", "© 2026 КОД ОТ TAN | ВСЕ ПРАВА ЗАЩИЩЕНЫ", "© 2026 CODIFICADO POR TAN | TODOS OS DIREITOS RESERVADOS", "© 2026 TAN کی طرف سے کوڈڈ | جملہ حقوق محفوظ ہیں", "© 2026 DIKODEKAN OLEH TAN | HAK CIPTA DILINDUNGI", "© 2026 CODIERT VON TAN | ALLE RECHTE VORBEHALTEN", "© 2026 TANによってコード化 | 無断転載禁止", "© 2026 TAN द्वारे कोड केलेले | सर्व हक्क राखीव", "© 2026 TAN ద్వారా కోడ్ చేయబడింది | సర్వ హక్కులు ప్రత్యేకించబడినవి", "© 2026 CODIFICATO DA TAN | TUTTI I DIRITTI RISERVATI", "© 2026 TAN ஆல் குறியிடப்பட்டது | அனைத்து உரிமைகளும் பாதுகாக்கப்பட்டவை", "© 2026 ĐƯỢC LẬP TRÌNH BỞI TAN | BẢN QUYỀN ĐƯỢC BẢO LƯU", "© 2026 TAN에 의해 코딩됨 | 모든 권리 보유", "© 2026 ZAKODOWANE PRZEZ TAN | WSZELKIE PRAWA ZASTRZEŻONE", "© 2026 GECODEERD DOOR TAN | ALLE RECHTE VOORBEHOUDEN", "© 2026 KODAD AV TAN | ALLA RÄTTIGHETER FÖRBEHÅLLNA", "© 2026 ΚΩΔΙΚΟΠΟΙΗΘΗΚΕ ΑΠΟ ΤΟΝ TAN | ΜΕ ΤΗΝ ΕΠΙΦΥΛΑΞΗ ΠΑΝΤΟΣ ΔΙΚΑΙΩΜΑΤΟΣ", "© 2026 NAKÓDOVÁNO OD TAN | VŠECHNA PRÁVA VYHRAZENA" } },
            { "Menu_Add", new[] { "Yeni Not", "New Note", "新笔记", "नया नोट", "Nueva Nota", "Nouvelle Note", "ملاحظة جديدة", "নতুন নোট", "Новая заметка", "Nova Nota", "نیا نوٹ", "Catatan Baru", "Neue Notiz", "新しいメモ", "नवीन नोट", "కొత్త నోట్", "Nuova Nota", "புதிய குறிப்பு", "Ghi chú mới", "새 메모", "Nowa Notatka", "Nieuwe Notitie", "Ny Anteckning", "Νέα Σημείωση", "Nová Poznámka" } },
            { "Menu_View", new[] { "Notları Gör", "View Notes", "查看笔记", "नोट्स देखें", "Ver Notas", "Voir Notes", "عرض الملاحظات", "নোট দেখুন", "Просмотр", "Ver Notas", "नोٹس देखें", "Lihat Catatan", "Ansehen", "メモを見る", "नोट्स पहा", "చూడండి", "Visualizza", "குறிப்புகளைப் பார்", "Xem Ghi Chú", "메모 보기", "Zobacz", "Bekijk", "Visa", "Προβολή", "Zobrazit" } },
            { "Menu_Edit", new[] { "Düzenle", "Edit Note", "编辑", "संपादित करें", "Editar", "Modifier", "تعديل", "সম্পাদনা", "Изменить", "Editar", "ترمیم", "Edit", "Bearbeiten", "編集", "संपादित करा", "సవరించu", "Modifica", "திருத்து", "Sửa", "편집", "Edytuj", "Bewerk", "Redigera", "Επεξεργασία", "Upravit" } },
            { "Menu_Del", new[] { "Not Sil", "Delete", "删除", "हटाएं", "Eliminar", "Supprimer", "حذف", "মুছে ফেলুন", "Удалить", "Excluir", "حذف کریں", "Hapus", "Löschen", "削除", "हटवा", "తొలగించు", "Elimina", "அழி", "Xóa", "삭제", "Usuń", "Verwijder", "Ta bort", "Διαγραφή", "Smazat" } },
            { "Menu_Search", new[] { "Arama Yap", "Search", "搜索", "खोजें", "Buscar", "Rechercher", "بحث", "অনুসন্ধান", "Поиск", "Pesquisar", "تلاش", "Cari", "Suchen", "検索", "शोधा", "శోధించండి", "Cerca", "தேடல்", "Tìm kiếm", "검색", "Szukaj", "Zoeken", "Sök", "Αναζήτηση", "Hledat" } },
            { "Menu_Stats", new[] { "İstatistikler", "Stats", "状态", "स्थिति", "Estado", "État", "الحالة", "স্ট্যাটাস", "Статус", "Status", "سٹیٹس", "Status", "Status", "状態", "स्थिती", "స్థితి", "Stato", "நிலை", "Trạng thái", "상태", "Status", "Status", "Status", "Κατάσταση", "Stav" } },
            { "Menu_Theme", new[] { "Temalar", "Themes", "主题", "विषय", "Temas", "Thèmes", "مواضيع", "থিম", "Темы", "Temas", "تھیمز", "Tema", "Themen", "テーマ", "थीम", "థీమ్స్", "Temi", "தீம்கள்", "Chủ đề", "테마", "Motywy", "Thema's", "Teman", "Θέματα", "Motivy" } },
            { "Menu_Lang", new[] { "Diller", "Languages", "语言", "भाषाएं", "Idiomas", "Langues", "لغات", "भाषा", "Языки", "Idiomas", "زبانیں", "Bahasa", "Sprachen", "言語", "भाषा", "భాషలు", "Lingue", "மொழிகள்", "Ngôn ngữ", "언어", "Języki", "Talen", "Språk", "Γλώσσες", "Jazyky" } },
            { "Menu_Exit", new[] { "Çıkış / Kapat", "Logout / Exit", "退出", "बाहر", "Salir", "Déconnexion", "خروج", "প্রস্থান", "Выход", "Sair", "خروج", "Keluar", "Abmelden", "終了", "बाहेर", "నిష్క్రమించు", "Esci", "வெளியேறு", "Thoát", "종료", "Wyloguj", "Uitloggen", "Logga ut", "Έξοδος", "Odhlásit se" } },
            { "UI_NavHelp", new[] { "[OKLAR] Gezin | [ENTER] Seç", "[ARROWS] Navigate | [ENTER] Select", "[箭头] 导航 | [ENTER] 选择", "[तीर] नेविगेट | [ENTER] चुनें", "[FLECHAS] Navegar | [ENTER] Sel", "[FLÈCHES] Naviguer | [ENTER] Sélec", "[أسهم] تنقل | [ENTER] تحديد", "[তীর] नेविगेट | [ENTER] নির্বাচন", "[СТРЕЛКИ] Навигация | [ENTER] Выбор", "[SETAS] Navegar | [ENTER] Sel", "[تیر] منتقل | [ENTER] منتخب", "[PANAH] Navigasi | [ENTER] Pilih", "[PFEILE] Navigieren | [ENTER] Wählen", "[矢印] ナビゲート | [ENTER] 選択", "[बाण] नेव्हिगेट | [ENTER] निवडा", "[బాణాలు] నావిగేట్ | [ENTER] ఎంచుకో", "[FRECCE] Naviga | [ENTER] Seleziona", "[அம்புகள்] நகர்த்துக | [ENTER] தேர்", "[MŨI TÊN] Điều hướng | [ENTER] Chọn", "[화살표] 이동 | [ENTER] 선택", "[STRZAŁKI] Nawiguj | [ENTER] Wybierz", "[PIJLEN] Nav | [ENTER] Kies", "[PILAR] Navigera | [ENTER] Välj", "[ΒΕΛΗ] Πλοήγηση | [ENTER] Επιλογή", "[ŠIPKY] Navigace | [ENTER] Výběr" } },
            { "UI_Save", new[] { "Kaydet", "Save", "保存", "सहेजना", "Guardar", "Sauver", "حفظ", "সংরক্ষণ", "Сохранить", "Salvar", "محفوظ کریں", "Simpan", "Speichern", "保存", "जतन करा", "సేవ్ చేయండి", "Salva", "சேமி", "Lưu", "저장", "Zapisz", "Opslaan", "Spara", "Αποθήκευση", "Uložit" } },
            { "UI_Cancel", new[] { "İptal", "Cancel", "取消", "रद्द करना", "Cancelar", "Annuler", "إلغاء", "বাতিল", "Отмена", "Cancelar", "منسوخ کریں", "Batal", "Abbrechen", "キャンセル", "रद्द करा", "రద్దు చేయి", "Annulla", "ரத்து", "Hủy", "취소", "Anuluj", "Annuleren", "Avbryt", "Ακύρωση", "Zrušit" } },
            { "UI_Title", new[] { "Başlık:", "Title:", "标题:", "शीर्षक:", "Título:", "Titre:", "العنوان:", "শিরোনাম:", "Заголовок:", "Título:", "عنوان:", "Judul:", "Titel:", "タイトル:", "शीर्षक:", "శీర్షిక:", "Titolo:", "தலைப்பு:", "Tiêu đề:", "제목:", "Tytuł:", "Titel:", "Titel:", "Τίτλος:", "Název:" } },
            { "UI_Content", new[] { "İçerik:", "Content:", "内容:", "सामग्री:", "Contenido:", "Contenu:", "المحتوى:", "विषयवस्तु:", "Содержание:", "Conteúdo:", "مواد:", "Konten:", "Inhalt:", "コンテンツ:", "सामग्री:", "కంటెంట్:", "Contenuto:", "உள்ளடக்கம்:", "Nội dung:", "내용:", "Treść:", "Inhoud:", "Innehåll:", "Περιεχόμενο:", "Obsah:" } },
            { "Stat_DB", new[] { "Veritabanı:", "Database:", "数据库:", "डेटाबेस:", "Base de datos:", "Base de données:", "قاعدة البيانات:", "ডাটাবেস:", "База данных:", "Banco de dados:", "ڈیٹا بیس:", "Basis Data:", "Datenbank:", "データベース:", "डेटाबेस:", "డేటాబేస్:", "Database:", "தரவுத்தளம்:", "Cơ sở dữ liệu:", "데이터베이스:", "Baza Danych:", "Database:", "Databas:", "Βάση δεδομένων:", "Databáze:" } },
            { "PressKey", new[] { "[ENTER] Devam Et", "[ENTER] Continue", "[ENTER]", "[ENTER]", "[ENTER]", "[ENTER]", "[ENTER]", "[ENTER]", "[ENTER]", "[ENTER]", "[ENTER]", "[ENTER]", "[ENTER]", "[ENTER]", "[ENTER]", "[ENTER]", "[ENTER]", "[ENTER]", "[ENTER]", "[ENTER]", "[ENTER]", "[ENTER]", "[ENTER]", "[ENTER]", "[ENTER]" } },
            { "Prompt_ID", new[] { "ID Girin:", "Enter ID:", "输入 ID:", "ID दर्ज करें:", "Ingresar ID:", "Entrer ID:", "أدخل المعرف:", "ID লিখুন:", "Введите ID:", "Inserir ID:", "آئی ڈی درج کریں:", "Masukkan ID:", "ID eingeben:", "IDを入力:", "ID प्रविष्ट करा:", "ID నమోదు చేయండి:", "Inserisci ID:", "ID-ஐ உள்ளிடவும்:", "Nhập ID:", "ID 입력:", "Wprowadź ID:", "Voer ID in:", "Ange ID:", "Εισάγετε ID:", "Zadejte ID:" } },
            { "Prompt_Confirm", new[] { "Onay (E/H):", "Confirm (Y/N):", "确认 (Y/N):", "पुष्टि (Y/N):", "Confirmar (S/N):", "Confirmer (O/N):", "تأكيد (Y/N):", "নিশ্চิต (Y/N):", "Подтвердить (Y/N):", "Confirmar (S/N):", "تصدیق (Y/N):", "Konfirmasi (Y/N):", "Bestätigen (J/N):", "確認 (Y/N):", "पुष्टी (Y/N):", "ధృవీకరించు (Y/N):", "Conferma (S/N):", "உறுதிப்படுத்தவும் (Y/N):", "Xác nhận (Y/N):", "확인 (Y/N):", "Potwierdź (T/N):", "Bevestig (J/N):", "Bekräfta (J/N):", "Επιβεβαίωση (Y/N):", "Potvrdit (A/N):" } },

            { "Th_01", new[] { "Siber Mavi", "Cyber Blue", "赛博蓝", "साइबर ब्लू", "Azul Ciber", "Bleu Cyber", "أزرق سيبراني", "সাইবার ব্লু", "Кибер-синий", "Azul Ciber", "سائبر بلیو", "Biru Siber", "Cyber Blau", "サイバーブルー", "सायबर निळा", "సైబర్ బ్లూ", "Blu Cyber", "சைபர் நீலம்", "Xanh Cyber", "사이버 블루", "Cyber Niebieski", "Cyber Blauw", "Cyber Blå", "Κυβερνομπλέ", "Kybernetická" } },
            { "Th_02", new[] { "Hacker Yeşili", "Hacker Green", "黑客绿", "हैकर ग्रीन", "Verde Hacker", "Vert Hacker", "أخضر هاكر", "হ্যাকার গ্রিন", "Хакерский", "Verde Hacker", "ہیکر گرین", "Hijau Hacker", "Hacker Grün", "ハッカーグリーン", "हॅकर हिरवा", "హ్యాకర్ గ్రీన్", "Verde Hacker", "ஹேக்கர் பச்சை", "Xanh Hacker", "해커 그린", "Hakerski", "Hacker Groen", "Hacker Grön", "Πράσινο Χάκερ", "Hacker" } },
            { "Th_03", new[] { "Kan Kırmızısı", "Blood Red", "血红", "रक्त लाल", "Rojo Sangre", "Rouge Sang", "أحمر دموي", "রক্ত लाल", "Кровавый", "Vermelho", "خون سرخ", "Merah Darah", "Blutrot", "ブラッドレッド", "رक्त लाल", "బ్లడ్ రెడ్", "Rosso Sangue", "சிவப்பு", "Đỏ Máu", "블러드 레드", "Krwista", "Bloedrood", "Blodröd", "Κόκκινο Αίματος", "Krvavá" } },
            { "Th_04", new[] { "Altın Kasa", "Gold Vault", "金库", "गोल्ड वॉルト", "Bóveda Oro", "Coffre d'Or", "خزنة ذهبية", "গোল্ড ভล্ট", "Золотой", "Cofre Ouro", "گولڈ والٹ", "Brankas Emas", "Gold Tresor", "ゴールドボルト", "गोल्ड वॉルト", "గోల్డ్ వాల్ట్", "Caveau d'Oro", "தங்க பெட்டகம்", "Kho Vàng", "골드 금고", "Złoty Skarbiec", "Gouden Kluis", "Guldvalv", "Χρυσό", "Zlatá" } },
            { "Th_05", new[] { "Derin Mor", "Deep Purple", "深紫", "गहरा बैंगनी", "Púrpura", "Violet Profond", "أرجواني عميق", "গভীর বেगुni", "Фиолетовый", "Roxo Profundo", "گہرا جامنی", "Ungu Tua", "Dunkellila", "ディープパープル", "गडद जांभळा", "డీప్ పర్పుల్", "Viola Profondo", "ஊதா", "Tím Sâu", "딥 퍼플", "Fiolet", "Diep Paars", "Djuplila", "Βαθύ Μωβ", "Fialová" } },
            { "Th_06", new[] { "Okyanus", "Ocean Wave", "海浪", "सागर की लहर", "Ola Oceano", "Vague d'Océan", "موج المحيط", "মহাসাগর", "Океан", "Onda Oceano", "سمندر کی لہر", "Ombak Laut", "Ozeanwelle", "オーシャン", "महासागर", "సముద్రపు అల", "Onda Oceanica", "கடல் அலை", "Sóng Biển", "오션 웨이브", "Fala Oceanu", "Ocean", "Oceanvåg", "Κύμα Ωκεανού", "Oceán" } },
            { "Th_07", new[] { "Orman", "Forest Leaf", "森林之叶", "वन पत्ता", "Hoja Bosque", "Feuille Forêt", "ورقة الغابة", "বনের পাতা", "Лес", "Folha Floresta", "جنگل کا پتہ", "Daun Hutan", "Waldblatt", "フォレスト", "जंगल पान", "అడవి ఆకు", "Foglia Bosco", "காட்டு இலை", "Lá Rừng", "포레스트 리프", "Leśny Liść", "Bos", "Skogsblad", "Φύλλο Δάσους", "Les" } },
            { "Th_08", new[] { "Gün Batımı", "Sunset Pink", "日落粉", "सूर्यास्त गुलाबी", "Ocaso Rosa", "Coucher Soleil", "غروب وردي", "সূর্যাস্ত", "Закат", "Pôr do Sol", "غروب آفتاب", "Merah Muda", "Sonnenuntergang", "サンセット", "सूर्यास्त", "సూర్యాస్తమయం", "Tramonto Rosa", "சூரிய அஸ்தமனம்", "Hoàng Hôn", "선셋 핑크", "Zachód Słońca", "Zonsondergang", "Solnedgång", "Ηλιοβασίλεμα", "Západ" } },
            { "Th_09", new[] { "Arktik Beyaz", "Arctic White", "北极白", "आर्कтик सफेद", "Blanco Ártico", "Blanc Arctique", "أبيض قطبي", "আর্কটিক সাদা", "Арктический", "Branco Ártico", "آرکٹک સફેદ", "Putih Arktik", "Arktisweiß", "アークティック", "पांढरा", "ఆర్కిటిక్ వైట్", "Bianco Artico", "ஆர்க்டிக் வெள்ளை", "Trắng Arctic", "아틱 화이트", "Arktyczna Biel", "Arctisch Wit", "Arktisk Vit", "Λευκό Αρκτικής", "Bílá" } },
            { "Th_10", new[] { "Çöl Kumu", "Desert Sand", "沙漠沙", "रेगिस्तान रेत", "Arena Desierto", "Sable Désert", "رمل الصحراء", "মরুভূমির বালি", "Пустыня", "Areia Deserto", "صحرائی ریت", "Pasir Gurun", "Wüstensand", "デザートサンド", "वाळू", "ఎడారి ఇసుక", "Sabbia Deserto", "பாலைவன மணல்", "Cát Sa Mạc", "데저트 샌드", "Piasek Pustyni", "Woestijnzand", "Ökensand", "Άμμος Ερήμου", "Písek" } },
            { "Th_11", new[] { "Gece Yarısı", "Midnight", "午夜", "आधी रात", "Medianoche", "Minuit", "منتصف الليل", "মধ্যরাত", "Полночь", "Meia-noite", "آدھی رات", "Tengah Malam", "Mitternacht", "ミッドナイト", "मध्यरात्र", "అర్ధరాత్రి", "Mezzanotte", "நள்ளிரவு", "Nửa Đêm", "미드나잇", "Północ", "Middernacht", "Midnatt", "Μεσάνυχτα", "Půlnoc" } },
            { "Th_12", new[] { "Nane", "Mint Neon", "薄荷霓虹", "पुदीना नियॉन", "Menta Neón", "Menthe Néon", "نعناع نيون", "মিন্ট নিয়ন", "Мятный неон", "Menta Neon", "منٹ نیین", "Mint Neon", "Minz-Neon", "ミントネオン", "पुदिना", "మింట్ నియాన్", "Menta Neon", "புதினா", "Bạc Hà Neon", "민트 네온", "Miętowy Neon", "Mint Neon", "Mintneon", "Νέον Μέντα", "Máta" } },
            { "Th_13", new[] { "Paslı Demir", "Rusty Iron", "生锈的铁", "जنگ لگا लोहा", "Hierro Oxidado", "Fer Rouillé", "حديد صدئ", "মরিচা ধরা লোহা", "Ржавое железо", "Ferro Enferrujado", "زنگ آلود لوہا", "Besi Berkarat", "Rostiges Eisen", "ラスティアイアン", "लोखंड", "తుప్పు పట్టిన ఇనుము", "Ferro Arrugginito", "இரும்பு", "Sắt Rỉ", "러스تي آیون", "Rdza", "Roestig IJzer", "Rostigt Järn", "Σκουριασμένο", "Železo" } },
            { "Th_14", new[] { "Elektrik", "Electric", "电力", "इलेक्ट्रिक", "Eléctrico", "Électrique", "كهربائي", "বৈদ্যুতিক", "Электрик", "Elétrico", "الیکٹرک", "Listrik", "Elektrisch", "エレクトリック", "इलेक्ट्रिक", "ఎలక్ట్రిక్", "Elettrico", "மின்சாரம்", "Điện", "일렉트릭", "Elektryczny", "Elektrisch", "Elektrisk", "Ηλεκτρικό", "Elektřina" } },
            { "Th_15", new[] { "Lavanta", "Lavender", "薰衣草", "लैवेंडर", "Lavanda", "Lavande", "خزامى", "ল্যাভেন্ডার", "Лаванда", "Lavanda", "لیوینڈر", "Lavender", "Lavendel", "ラベンダー", "लॅव्हेंडर", "లావెండర్", "Lavanda", "லாவெண்டர்", "Oải Hương", "라벤더", "Lawenda", "Lavendel", "Lavendel", "Λεβάντα", "Levandule" } },
            { "Th_16", new[] { "Uzay Grisi", "Space Grey", "太空灰", "स्पेस ग्रे", "Gris Espacial", "Gris Spatial", "رمادي فضائي", "স্পেস গ্রে", "Космический", "Cinza Espacial", "خلائی سرمئی", "Abu-abu Ruang", "Spacegrau", "スペースグレイ", "ग्रे", "స్పే스 Гரே", "Grigio Spazio", "சாம்பல்", "Xám Không Gian", "스페이스 그레이", "Gwiezdna Szarość", "Space Grey", "Rymdgrå", "Γκρι του Διαστήματος", "Vesmírná" } },
            { "Th_17", new[] { "Zehirli", "Toxic", "毒性", "विषाक्त", "Tóxico", "Toxique", "سام", "বিষাক্ত", "Токсичный", "Tóxico", "زہریلا", "Beracun", "Toxisch", "トキシック", "विषारी", "టాక్సిక్", "Tossico", "நச்சு", "Độc Hại", "톡식", "Toksyczny", "Toxisch", "Toxisk", "Toξικό", "Toxická" } },
            { "Th_18", new[] { "Gökyüzü", "Sky Blue", "天蓝", "आसमान नीला", "Azul Cielo", "Bleu Ciel", "أزرق سماوي", "আকাশী", "Небесный", "Azul Céu", "آسمانی نیلا", "Biru Langit", "Himmelblau", "スカイブルー", "निळा", "స్కై బ్లూ", "Azzurro", "வான நீலம்", "Xanh Trời", "스카이 블루", "Błękitny", "Hemelsblauw", "Himmelblå", "Γαλάζιο", "Obloha" } },
            { "Th_19", new[] { "Vişne", "Maroon", "栗色", "मैरून", "Granate", "Marron", "مارون", "মেরুন", "Бордовый", "Castanho", "میرون", "Maroon", "Kastanienbraun", "マルーン", "मॅरून", "मरूન", "Amaranto", "மெரூன்", "Đỏ Đô", "마룬", "Bordowy", "Marron", "Vinröd", "Βυσσινί", "Kaštanová" } },
            { "Th_20", new[] { "Zeytin", "Olive", "橄榄", "जैतून", "Oliva", "Olive", "زيتوني", "জলপাই", "Оливковый", "Oliva", "زیتونی", "Zaitun", "Olivgrün", "オリーブ", "ऑलिव्ह", "ఆలివ్", "Oliva", "ஆలివ్", "Ô Liu", "올리브", "Oliwkowy", "Olijf", "Oliv", "Ελιά", "Olivová" } },
            { "Th_21", new[] { "Turkuaz Gece", "Teal Night", "蓝绿色之夜", "टील नाइट", "Noche Teal", "Nuit Teal", "ليلة تيل", "তিল নাইট", "Бирюзовая ночь", "Noite Teal", "ٹیل نائٹ", "Teal Malam", "Teal Nacht", "ティールナイト", "नाईट", "టీల్ నైట్", "Notte Teal", "டீல் நைட்", "Đêm Teal", "틸 나이트", "Turkusowa Noc", "Teal Nacht", "Teal Natt", "Τυρκουάζ Νύχτα", "Tyrkysová" } },
            { "Th_22", new[] { "Kıpkırmızı", "Crimson", "深红色", "गहरा लाल", "Carmesí", "Cramoisi", "قرمزي", "ক্রিমসন", "Малиновый", "Carmesim", "کرائمسن", "Crimson", "Karmesin", "クリムゾン", "लाल", "క్రిమ్సన్", "Cremisi", "கருஞ்சிவப்பு", "Đỏ Thẫm", "크림슨", "Karmazynowy", "Karmijn", "Karmosin", "Crimson", "Purpurová" } },
            { "Th_23", new[] { "Gümüş", "Silver", "银色", "चांदी", "Plata", "Argent", "فضي", "রূপালী", "Серебряный", "Prata", "چاندی", "Perak", "Silber", "シルバー", "चांदी", "సిల్వర్", "Argento", "வெள்ளி", "Bạc", "실버", "Srebrny", "Zilver", "Silver", "Ασημί", "Stříbrná" } },
            { "Th_24", new[] { "Kehribar", "Amber", "琥珀色", "एम्बर", "Ámbar", "Ambre", "كهرمان", "অ্যাম্বার", "Янтарный", "Âmbar", "امبر", "Amber", "Bernstein", "アンバー", "एम्बर", "అంబర్", "Ambra", "அம்பர்", "Hổ Phách", "앰버", "Bursztynowy", "Amber", "Bärnsten", "Κεχριμπάρι", "Jantar" } },
            { "Th_25", new[] { "Erik", "Plum", "梅色", "प्लम", "Ciruela", "Prune", "برقوقي", "প্লাম", "Сливовый", "Ameixa", "پلم", "Plum", "Pflaume", "プラム", "प्लम", "ప్లం", "Prugna", "பிளம்", "Mận", "플럼", "Śliwkowy", "Pruim", "Plommon", "Δαμασκηνί", "Švestková" } },
            { "Th_26", new[] { "Kraliyet Mavisi", "Royal Blue", "皇室蓝", "रॉयल ब्लू", "Azul Real", "Bleu Royal", "أzرق ملكي", "রয়েল ব্লু", "Королевский", "Azul Royal", "رائل بلیو", "Biru Kerajaan", "Königsblau", "ロイヤルブルー", "रॉयल निळा", "రాయల్ బ్లూ", "Blu Reale", "ராயல் நீலம்", "Xanh Hoàng Gia", "로열 블루", "Królewski Niebieski", "Koningsblauw", "Kungsblå", "Βασιλικό Μπλε", "Královská" } },
            { "Th_27", new[] { "Zümrüt", "Emerald", "祖母绿", "पन्ना", "Esmeralda", "Émeraude", "زمردي", "পান্না", "Изумрудный", "Esmeralda", "زمرد", "Zamrud", "Smaragd", "エメラルド", "पाचू", "ఎమరాల్డ్", "Smeraldo", "மரகதம்", "Lục Bảo", "에메랄드", "Szmaragdowy", "Smaragd", "Smaragd", "Σμαραγδί", "Smaragd" } },
            { "Th_28", new[] { "Kömür", "Coal", "煤色", "कोयला", "Carbón", "Charbon", "فحم", "কয়লা", "Уголь", "Carvão", "کوئلہ", "Batu Bara", "Kohle", "コール", "कोळसा", "బొగ్గు", "Carbone", "நிலக்கरी", "Than", "콜", "Węglowy", "Steenkool", "Kol", "Άνθρακας", "Uhlí" } },
            { "Th_29", new[] { "Matrix", "Matrix", "矩阵", "मैट्रिक्स", "Matriz", "Matrice", "مصفوفة", "ম্যাট্রিক্স", "Матрица", "Matriz", "میٹرکس", "Matriks", "Matrix", "マトリックス", "मॅट्रिक्स", "మాట్రిక్స్", "Matrice", "மேட்ரிக்ஸ்", "Ma Trận", "매트릭스", "Matrix", "Matrix", "Matrix", "Matrix", "Matrix" } },
            { "Th_30", new[] { "Klasik", "Classic", "经典", "क्लासिक", "Clásico", "Classique", "كلاسيكي", "ক্লাসিক", "Классика", "Clássico", "کلاسک", "Klasik", "Klassisch", "クラシック", "क्लासिक", "క్లాసిక్", "Classico", "கிளாசிக்", "Cổ Điển", "클래식", "Klasyczny", "Klassiek", "Klassisk", "Κλασικό", "Klasika" } }
        };

        public static string Get(string key)
        {
            if (dict.ContainsKey(key)) return dict[key][CurrentLang - 1];
            return $"[{key}]";
        }

        public static string[] GetThemeNames()
        {
            string[] names = new string[30];
            for (int i = 1; i <= 30; i++) names[i - 1] = Get($"Th_{i:D2}");
            return names;
        }
    }
    #endregion

    #region 5. DİNAMİK TUI MOTORU (RENKLER VE GİRİŞ)
    public static class TUI
    {
        public static ConsoleColor ColorBg = ConsoleColor.Black;
        public static ConsoleColor ColorBorder = ConsoleColor.DarkGray;
        public static ConsoleColor ColorAccent = ConsoleColor.Cyan;
        public static ConsoleColor ColorText = ConsoleColor.White;
        public static ConsoleColor ColorDim = ConsoleColor.Gray;

        public static int Width => Math.Max(Console.WindowWidth, 60);
        public static int Height => Math.Max(Console.WindowHeight, 20);

        public static void ApplyTheme(int themeId)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            ColorBg = ConsoleColor.Black;
            ColorText = ConsoleColor.White;
            ColorDim = ConsoleColor.Gray;

            switch (themeId)
            {
                case 1: ColorBorder = ConsoleColor.DarkBlue; ColorAccent = ConsoleColor.Cyan; break;
                case 2: ColorBorder = ConsoleColor.DarkGreen; ColorAccent = ConsoleColor.Green; break;
                case 3: ColorBorder = ConsoleColor.DarkRed; ColorAccent = ConsoleColor.Red; break;
                case 4: ColorBorder = ConsoleColor.DarkYellow; ColorAccent = ConsoleColor.Yellow; break;
                case 5: ColorBorder = ConsoleColor.DarkMagenta; ColorAccent = ConsoleColor.Magenta; break;
                case 6: ColorBorder = ConsoleColor.DarkBlue; ColorAccent = ConsoleColor.Blue; break;
                case 7: ColorBorder = ConsoleColor.DarkGreen; ColorAccent = ConsoleColor.Green; break;
                case 8: ColorBorder = ConsoleColor.DarkRed; ColorAccent = ConsoleColor.Magenta; break;
                case 9: ColorBorder = ConsoleColor.Gray; ColorAccent = ConsoleColor.White; break;
                case 10: ColorBorder = ConsoleColor.DarkYellow; ColorAccent = ConsoleColor.Yellow; break;
                case 11: ColorBorder = ConsoleColor.Black; ColorAccent = ConsoleColor.DarkGray; break;
                case 12: ColorBorder = ConsoleColor.Green; ColorAccent = ConsoleColor.Cyan; break;
                case 13: ColorBorder = ConsoleColor.DarkRed; ColorAccent = ConsoleColor.DarkYellow; break;
                case 14: ColorBorder = ConsoleColor.Blue; ColorAccent = ConsoleColor.Yellow; break;
                case 15: ColorBorder = ConsoleColor.Magenta; ColorAccent = ConsoleColor.White; break;
                case 16: ColorBorder = ConsoleColor.DarkGray; ColorAccent = ConsoleColor.Gray; break;
                case 17: ColorBorder = ConsoleColor.DarkGreen; ColorAccent = ConsoleColor.Yellow; break;
                case 18: ColorBorder = ConsoleColor.Blue; ColorAccent = ConsoleColor.Cyan; break;
                case 19: ColorBorder = ConsoleColor.DarkRed; ColorAccent = ConsoleColor.Red; break;
                case 20: ColorBorder = ConsoleColor.DarkYellow; ColorAccent = ConsoleColor.Green; break;
                case 21: ColorBorder = ConsoleColor.DarkCyan; ColorAccent = ConsoleColor.Cyan; break;
                case 22: ColorBorder = ConsoleColor.Red; ColorAccent = ConsoleColor.DarkRed; break;
                case 23: ColorBorder = ConsoleColor.Gray; ColorAccent = ConsoleColor.White; break;
                case 24: ColorBorder = ConsoleColor.DarkYellow; ColorAccent = ConsoleColor.Yellow; break;
                case 25: ColorBorder = ConsoleColor.DarkMagenta; ColorAccent = ConsoleColor.Magenta; break;
                case 26: ColorBorder = ConsoleColor.Blue; ColorAccent = ConsoleColor.DarkBlue; break;
                case 27: ColorBorder = ConsoleColor.Green; ColorAccent = ConsoleColor.DarkGreen; break;
                case 28: ColorBorder = ConsoleColor.Black; ColorAccent = ConsoleColor.Gray; break;
                case 29: ColorBorder = ConsoleColor.DarkGreen; ColorAccent = ConsoleColor.Green; ColorText = ConsoleColor.Green; break;
                case 30: ColorBorder = ConsoleColor.DarkGray; ColorAccent = ConsoleColor.White; break;
                default: ColorBorder = ConsoleColor.DarkGray; ColorAccent = ConsoleColor.Cyan; break;
            }
        }

        public static void DrawFrame(int x, int y, int w, int h, string title = "")
        {
            if (w <= 0 || h <= 0) return;
            Console.ForegroundColor = ColorBorder;
            try
            {
                Console.SetCursorPosition(x, y); Console.Write("┌" + new string('─', w - 2) + "┐");
                for (int i = 1; i < h - 1; i++)
                {
                    Console.SetCursorPosition(x, y + i); Console.Write("│");
                    Console.SetCursorPosition(x + w - 1, y + i); Console.Write("│");
                }
                Console.SetCursorPosition(x, y + h - 1); Console.Write("└" + new string('─', w - 2) + "┘");
                if (!string.IsNullOrEmpty(title))
                {
                    Console.SetCursorPosition(x + 2, y);
                    Console.ForegroundColor = ColorAccent;
                    Console.Write($" {title} ");
                }
            }
            catch { }
        }

        public static void WriteAt(int x, int y, string text, ConsoleColor color)
        {
            if (x < 0 || y < 0 || x >= Console.WindowWidth || y >= Console.WindowHeight) return;
            try { Console.SetCursorPosition(x, y); Console.ForegroundColor = color; Console.Write(text); } catch { }
        }

        public static string InputAt(int x, int y, int maxLength, string prompt, bool isPassword = false)
        {
            WriteAt(x, y, prompt, ConsoleColor.Yellow);
            int inputX = x + prompt.Length + 1;
            StringBuilder input = new StringBuilder();
            Console.CursorVisible = true;
            int cursorPos = 0;
            while (true)
            {
                Console.SetCursorPosition(inputX, y);
                string display = isPassword ? new string('*', input.Length) : input.ToString();
                Console.Write(display.PadRight(maxLength) + " ");
                Console.SetCursorPosition(inputX + cursorPos, y);
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter) break;
                if (key.Key == ConsoleKey.Escape) { Console.CursorVisible = false; return null; }
                if (key.Key == ConsoleKey.Backspace && cursorPos > 0) { input.Remove(cursorPos - 1, 1); cursorPos--; }
                else if (!char.IsControl(key.KeyChar) && input.Length < maxLength) { input.Insert(cursorPos, key.KeyChar); cursorPos++; }
            }
            Console.CursorVisible = false; return input.ToString().Trim();
        }

        public static string InputMultiLine(int x, int y, int w, int h, string prompt)
        {
            string hint = $"[F1: {LangEngine.Get("UI_Save")} / ESC: {LangEngine.Get("UI_Cancel")}]";
            WriteAt(x, y, prompt + " " + hint, ConsoleColor.Yellow);
            y++; int curX = x, curY = y;
            Console.ForegroundColor = ColorText;
            Console.CursorVisible = true;
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                Console.SetCursorPosition(curX, curY);
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.F1) break;
                if (key.Key == ConsoleKey.Escape) { Console.CursorVisible = false; return null; }
                if (key.Key == ConsoleKey.Enter)
                {
                    sb.Append(Environment.NewLine);
                    curX = x; curY++;
                    if (curY >= y + h) break;
                    continue;
                }
                if (key.Key == ConsoleKey.Backspace && sb.Length > 0)
                {
                    if (sb.Length >= Environment.NewLine.Length && sb.ToString().EndsWith(Environment.NewLine))
                    {
                        sb.Remove(sb.Length - Environment.NewLine.Length, Environment.NewLine.Length);
                        curY--;
                        var lines = sb.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                        curX = x + (lines.Last().Length);
                    }
                    else
                    {
                        sb.Length--; curX--;
                        if (curX < x) { curY--; curX = x + w - 1; }
                    }
                    Console.SetCursorPosition(curX, curY); Console.Write(" ");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    sb.Append(key.KeyChar); Console.Write(key.KeyChar);
                    curX++;
                    if (curX >= x + w) { curX = x; curY++; if (curY >= y + h) break; }
                }
            }
            Console.CursorVisible = false; return sb.ToString().TrimEnd();
        }

        public static int InteractiveMenu(int x, int y, string[] options, int currentIndex)
        {
            while (true)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    Console.SetCursorPosition(x, y + i);
                    if (i == currentIndex)
                    {
                        Console.BackgroundColor = ColorAccent; Console.ForegroundColor = ConsoleColor.Black;
                        Console.Write($" ► {options[i].PadRight(22)}");
                        Console.BackgroundColor = ColorBg;
                    }
                    else
                    {
                        Console.ForegroundColor = ColorDim;
                        Console.Write($"   {options[i].PadRight(22)}");
                    }
                }
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.DownArrow && currentIndex < options.Length - 1) currentIndex++;
                else if (key.Key == ConsoleKey.UpArrow && currentIndex > 0) currentIndex--;
                else if (key.Key == ConsoleKey.Enter) return currentIndex;
                else if (key.Key == ConsoleKey.Escape) return -1;
            }
        }

        public static int InteractiveGridSelector(int startX, int startY, string[] options, int currentIndex, int cols, int maxW)
        {
            int colWidth = maxW / cols;
            while (true)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    int r = i / cols, c = i % cols;
                    int dx = startX + (c * colWidth), dy = startY + r;
                    Console.SetCursorPosition(dx, dy);
                    if (i == currentIndex)
                    {
                        Console.BackgroundColor = ColorAccent; Console.ForegroundColor = ConsoleColor.Black;
                        Console.Write($" {options[i].PadRight(colWidth - 2)} ");
                        Console.BackgroundColor = ColorBg;
                    }
                    else
                    {
                        Console.ForegroundColor = ColorDim;
                        Console.Write($" {options[i].PadRight(colWidth - 2)} ");
                    }
                }
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.RightArrow && currentIndex < options.Length - 1) currentIndex++;
                else if (key.Key == ConsoleKey.LeftArrow && currentIndex > 0) currentIndex--;
                else if (key.Key == ConsoleKey.DownArrow && currentIndex + cols < options.Length) currentIndex += cols;
                else if (key.Key == ConsoleKey.UpArrow && currentIndex - cols >= 0) currentIndex -= cols;
                else if (key.Key == ConsoleKey.Enter) return currentIndex;
                else if (key.Key == ConsoleKey.Escape) return -1;
            }
        }

        public static void ClearArea(int x, int y, int width, int height)
        {
            for (int i = 0; i < height; i++)
            {
                try { Console.SetCursorPosition(x, y + i); Console.Write(new string(' ', width)); } catch { }
            }
        }
    }
    #endregion

    #region 6. DEPOLAMA MOTORU
    public static class StorageEngine
    {
        private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TNotesVault");
        private static string ConfFilePath => Path.Combine(AppDataFolder, "TNotes_AppConfig.xml");

        public static void EnsureDir() { if (!Directory.Exists(AppDataFolder)) Directory.CreateDirectory(AppDataFolder); }
        public static AppConfig LoadConfig()
        {
            EnsureDir();
            if (!File.Exists(ConfFilePath)) return new AppConfig();
            try { using (var sr = new StreamReader(ConfFilePath)) return (AppConfig)new XmlSerializer(typeof(AppConfig)).Deserialize(sr); }
            catch { return new AppConfig(); }
        }
        public static void SaveConfig(AppConfig c) { EnsureDir(); try { using (var sw = new StreamWriter(ConfFilePath)) new XmlSerializer(typeof(AppConfig)).Serialize(sw, c); } catch { } }
        public static List<NoteEntry> LoadDB(string u)
        {
            EnsureDir(); string f = Path.Combine(AppDataFolder, $"TNotes_{u.ToUpper()}.xml");
            if (!File.Exists(f)) return new List<NoteEntry>();
            try { using (var sr = new StreamReader(f)) return (List<NoteEntry>)new XmlSerializer(typeof(List<NoteEntry>)).Deserialize(sr); }
            catch { return new List<NoteEntry>(); }
        }
        public static void SaveDB(string u, List<NoteEntry> n) { EnsureDir(); string f = Path.Combine(AppDataFolder, $"TNotes_{u.ToUpper()}.xml"); try { using (var sw = new StreamWriter(f)) new XmlSerializer(typeof(List<NoteEntry>)).Serialize(sw, n); } catch { } }
    }
    #endregion

    #region 7. ANA DENETLEYİCİ (STATE MACHINE)
    class Program
    {
        static List<NoteEntry> db = new List<NoteEntry>();
        static AppConfig config;
        static UserAccount CurrentUser;
        static int selectedMenuIndex = 0;
        static readonly string[] menuKeys = { "Menu_Add", "Menu_View", "Menu_Edit", "Menu_Del", "Menu_Search", "Menu_Stats", "Menu_Theme", "Menu_Lang", "Menu_Exit" };

        static int WorkspaceX => 26;
        static int WorkspaceY => 4;
        static int WorkspaceW => TUI.Width - 28;
        static int WorkspaceH => TUI.Height - 8;

        static void Main()
        {
            WindowManager.OptimizeConsole();
            WindowManager.BootAnim();
            config = StorageEngine.LoadConfig();
            LangEngine.ChangeLanguage(config.SystemLanguageId);
            TUI.ApplyTheme(1);

            while (true)
            {
                if (!FlowBootScreen()) break;
                LangEngine.ChangeLanguage(CurrentUser.LanguageId);
                TUI.ApplyTheme(CurrentUser.ThemeId);
                db = StorageEngine.LoadDB(CurrentUser.Username);
                RunEventLoop();
                StorageEngine.SaveDB(CurrentUser.Username, db);
                CurrentUser = null;
            }
            Environment.Exit(0);
        }

        static bool FlowBootScreen()
        {
            int idx = 0; bool redraw = true;
            while (true)
            {
                int cx = TUI.Width / 2, cy = TUI.Height / 2;
                if (redraw)
                {
                    Console.BackgroundColor = TUI.ColorBg; Console.Clear();
                    TUI.DrawFrame(0, 0, TUI.Width, TUI.Height, LangEngine.Get("Auth_Title"));

                    string[] logo = {
                        "  _______   _   _  ____ _______ ______  _____ ",
                        " |__   __| | \\ | |/ __ \\__   __|  ____|/ ____|",
                        "    | |    |  \\| | |  | | | |  | |__  | (___  ",
                        "    | |    | . ` | |  | | | |  |  __|  \\___ \\ ",
                        "    | |    | |\\  | |__| | | |  | |____ ____) |",
                        "    |_|    |_| \\_|\\____/  |_|  |______|_____/ ",
                        "                                              ",
                        "                        V  A  U  L  T    S  Y  S  T  E  M    "
                    };

                    int logoY = cy - 10;
                    for (int i = 0; i < logo.Length; i++) TUI.WriteAt((TUI.Width - logo[i].Length) / 2, logoY + i, logo[i], TUI.ColorAccent);

                    string motto = $"- {LangEngine.Get("App_Motto")} -";
                    TUI.WriteAt((TUI.Width - motto.Length) / 2, cy - 1, motto, TUI.ColorDim);

                    string copy = $" {LangEngine.Get("Copyright")} ";
                    TUI.WriteAt((TUI.Width - copy.Length) / 2, TUI.Height - 2, copy, ConsoleColor.DarkYellow);
                    redraw = false;
                }

                string[] opts = { LangEngine.Get("Auth_LoginBtn"), LangEngine.Get("Auth_RegBtn"), LangEngine.Get("Menu_Lang"), LangEngine.Get("Menu_Exit") };
                idx = TUI.InteractiveMenu(cx - 11, cy + 2, opts, idx);

                if (idx == -1 || idx == 3) return false;
                if (idx == 0) // Login
                {
                    TUI.ClearArea(cx - 20, cy + 1, 40, 8);
                    string u = TUI.InputAt(cx - 15, cy + 2, 20, LangEngine.Get("Auth_User"));
                    if (u == null) { redraw = true; continue; }
                    var exist = config.Users.FirstOrDefault(x => x.Username.Equals(u, StringComparison.OrdinalIgnoreCase));
                    if (exist == null) { TUI.WriteAt(cx - 10, cy + 5, LangEngine.Get("Auth_NoUser"), ConsoleColor.Red); Thread.Sleep(1200); redraw = true; continue; }
                    string p = TUI.InputAt(cx - 15, cy + 4, 20, LangEngine.Get("Auth_Pass"), true);
                    if (p == null) { redraw = true; continue; }
                    if (SecurityEngine.HashPassword(p) == exist.PasswordHash) { CurrentUser = exist; return true; }
                    else { TUI.WriteAt(cx - 7, cy + 5, LangEngine.Get("Auth_Error"), ConsoleColor.Red); Thread.Sleep(1200); redraw = true; }
                }
                else if (idx == 1) // Register
                {
                    TUI.ClearArea(cx - 20, cy + 1, 40, 8);
                    string u = TUI.InputAt(cx - 15, cy + 2, 20, LangEngine.Get("Auth_User"));
                    if (u == null) { redraw = true; continue; }
                    if (config.Users.Any(x => x.Username.Equals(u, StringComparison.OrdinalIgnoreCase))) { TUI.WriteAt(cx - 12, cy + 5, LangEngine.Get("Auth_Exists"), ConsoleColor.Red); Thread.Sleep(1200); redraw = true; continue; }
                    string p = TUI.InputAt(cx - 15, cy + 4, 20, LangEngine.Get("Auth_Pass"), true);
                    if (p == null || p.Length < 3) { redraw = true; continue; }
                    config.Users.Add(new UserAccount { Username = u, PasswordHash = SecurityEngine.HashPassword(p), LanguageId = config.SystemLanguageId });
                    StorageEngine.SaveConfig(config);
                    TUI.WriteAt(cx - 8, cy + 5, LangEngine.Get("Auth_RegOK"), ConsoleColor.Green); Thread.Sleep(1200); redraw = true;
                }
                else if (idx == 2) // Lang
                {
                    int sl = TUI.InteractiveGridSelector(cx - 23, cy + 1, LangEngine.LanguageNames, config.SystemLanguageId - 1, 3, 46);
                    if (sl != -1) { config.SystemLanguageId = sl + 1; LangEngine.ChangeLanguage(config.SystemLanguageId); StorageEngine.SaveConfig(config); }
                    redraw = true;
                }
            }
        }

        static void RenderLayout()
        {
            Console.BackgroundColor = TUI.ColorBg; Console.Clear();
            TUI.DrawFrame(0, 0, TUI.Width, 3, "T NOTES VAULT");
            string time = DateTime.Now.ToString("dd MMM yyyy - HH:mm", LangEngine.GetCulture());
            TUI.WriteAt(2, 1, $"{LangEngine.Get("Auth_UserLabel")}: {CurrentUser.Username.ToUpper()} | SECURE KERNEL", TUI.ColorDim);
            TUI.WriteAt(TUI.Width - time.Length - 4, 1, time, ConsoleColor.Yellow);
            TUI.DrawFrame(0, 3, 25, TUI.Height - 6, "MENU");
            TUI.DrawFrame(25, 3, TUI.Width - 25, TUI.Height - 6, "WORKSPACE");
            TUI.DrawFrame(0, TUI.Height - 3, TUI.Width, 3, "STATUS");
            TUI.WriteAt(2, TUI.Height - 2, LangEngine.Get("UI_NavHelp"), TUI.ColorDim);
        }

        static void RunEventLoop()
        {
            RenderLayout();
            while (true)
            {
                for (int i = 0; i < menuKeys.Length; i++)
                {
                    TUI.WriteAt(2, 5 + (i * 2), (i == selectedMenuIndex ? " ► " : "   ") + LangEngine.Get(menuKeys[i]).PadRight(18), i == selectedMenuIndex ? TUI.ColorAccent : TUI.ColorDim);
                }
                var k = Console.ReadKey(true);
                if (k.Key == ConsoleKey.UpArrow) { selectedMenuIndex--; if (selectedMenuIndex < 0) selectedMenuIndex = menuKeys.Length - 1; }
                else if (k.Key == ConsoleKey.DownArrow) { selectedMenuIndex++; if (selectedMenuIndex >= menuKeys.Length) selectedMenuIndex = 0; }
                else if (k.Key == ConsoleKey.Enter)
                {
                    if (selectedMenuIndex == 8) break;
                    ExecuteAction(); RenderLayout();
                }
            }
        }

        static void ExecuteAction()
        {
            TUI.ClearArea(WorkspaceX + 2, WorkspaceY + 2, WorkspaceW - 4, WorkspaceH - 4);
            int cy = WorkspaceY + 2;
            switch (selectedMenuIndex)
            {
                case 0: // Add
                    string t = TUI.InputAt(WorkspaceX + 2, cy, WorkspaceW - 12, "> " + LangEngine.Get("UI_Title"));
                    if (t != null)
                    {
                        string c = TUI.InputMultiLine(WorkspaceX + 2, cy + 2, WorkspaceW - 6, WorkspaceH - 10, "> " + LangEngine.Get("UI_Content"));
                        if (c != null) { db.Add(new NoteEntry { Title = t.ToUpper(), EncryptedContent = SecurityEngine.Encrypt(c) }); StorageEngine.SaveDB(CurrentUser.Username, db); }
                    }
                    break;
                case 1: // View
                    FlowView(db.OrderByDescending(x => x.CreatedAt).ToList()); break;
                case 2: // Edit
                    string id = TUI.InputAt(WorkspaceX + 2, cy, 6, LangEngine.Get("Prompt_ID"));
                    var note = db.FirstOrDefault(x => x.Id == id?.ToUpper());
                    if (note != null)
                    {
                        string nt = TUI.InputAt(WorkspaceX + 2, cy + 2, 20, "> " + LangEngine.Get("UI_Title"));
                        string nc = TUI.InputMultiLine(WorkspaceX + 2, cy + 4, WorkspaceW - 6, WorkspaceH - 12, "> " + LangEngine.Get("UI_Content"));
                        if (nc != null) { note.Title = string.IsNullOrWhiteSpace(nt) ? note.Title : nt.ToUpper(); note.EncryptedContent = SecurityEngine.Encrypt(nc); StorageEngine.SaveDB(CurrentUser.Username, db); }
                    }
                    break;
                case 3: // Delete
                    string di = TUI.InputAt(WorkspaceX + 2, cy, 6, LangEngine.Get("Prompt_ID"));
                    var dn = db.FirstOrDefault(x => x.Id == di?.ToUpper());
                    if (dn != null)
                    {
                        string conf = TUI.InputAt(WorkspaceX + 2, cy + 2, 1, LangEngine.Get("Prompt_Confirm"));
                        if (conf?.ToUpper() == "Y" || conf?.ToUpper() == "E") { db.Remove(dn); StorageEngine.SaveDB(CurrentUser.Username, db); }
                    }
                    break;
                case 4: // Search
                    string q = TUI.InputAt(WorkspaceX + 2, cy, 20, "> " + LangEngine.Get("Menu_Search") + ":");
                    if (q != null) FlowView(db.Where(x => x.Title.Contains(q.ToUpper()) || SecurityEngine.Decrypt(x.EncryptedContent).ToLower().Contains(q.ToLower())).ToList());
                    break;
                case 5: // Stats
                    TUI.WriteAt(WorkspaceX + 2, cy, $"{LangEngine.Get("Stat_DB")} {db.Count}", TUI.ColorText);
                    TUI.WriteAt(WorkspaceX + 2, cy + 2, $"Theme: {LangEngine.GetThemeNames()[CurrentUser.ThemeId - 1]}", TUI.ColorAccent);
                    TUI.WriteAt(WorkspaceX + 2, cy + 3, $"Lang: {LangEngine.LanguageNames[CurrentUser.LanguageId - 1]}", TUI.ColorDim);
                    break;
                case 6: // Theme
                    int st = TUI.InteractiveGridSelector(WorkspaceX + 2, cy, LangEngine.GetThemeNames(), CurrentUser.ThemeId - 1, 2, WorkspaceW - 4);
                    if (st != -1)
                    {
                        CurrentUser.ThemeId = st + 1;
                        config.Users.First(u => u.Username == CurrentUser.Username).ThemeId = st + 1;
                        StorageEngine.SaveConfig(config);
                        TUI.ApplyTheme(st + 1);
                    }
                    break;
                case 7: // Lang
                    int sl = TUI.InteractiveGridSelector(WorkspaceX + 2, cy, LangEngine.LanguageNames, CurrentUser.LanguageId - 1, 2, WorkspaceW - 4);
                    if (sl != -1)
                    {
                        CurrentUser.LanguageId = sl + 1;
                        LangEngine.ChangeLanguage(sl + 1);
                        config.Users.First(u => u.Username == CurrentUser.Username).LanguageId = sl + 1;
                        StorageEngine.SaveConfig(config);
                    }
                    break;
            }
            if (selectedMenuIndex != 1 && selectedMenuIndex != 4 && selectedMenuIndex != 6 && selectedMenuIndex != 7)
            {
                TUI.WriteAt(WorkspaceX + 2, WorkspaceY + WorkspaceH - 2, LangEngine.Get("PressKey"), TUI.ColorDim);
                Console.ReadKey(true);
            }
        }

        static void FlowView(List<NoteEntry> list)
        {
            int p = 0, size = (WorkspaceH - 6) / 4;
            if (list.Count == 0) { TUI.WriteAt(WorkspaceX + 2, WorkspaceY + 2, "--- NO DATA ---", ConsoleColor.Yellow); Console.ReadKey(true); return; }
            while (true)
            {
                TUI.ClearArea(WorkspaceX + 1, WorkspaceY + 1, WorkspaceW - 2, WorkspaceH - 3);
                var items = list.Skip(p * size).Take(size); int y = WorkspaceY + 2;
                foreach (var n in items)
                {
                    TUI.WriteAt(WorkspaceX + 2, y, $"[{n.Id}] {n.Title}", TUI.ColorAccent);
                    string dec = SecurityEngine.Decrypt(n.EncryptedContent);
                    TUI.WriteAt(WorkspaceX + 2, y + 1, "> " + (dec.Length > WorkspaceW - 10 ? dec.Substring(0, WorkspaceW - 10) + "..." : dec), TUI.ColorText);
                    y += 3;
                }
                TUI.WriteAt(WorkspaceX + 2, WorkspaceY + WorkspaceH - 2, "[LEFT/RIGHT] Page | [ESC] Back", TUI.ColorDim);
                var k = Console.ReadKey(true);
                if (k.Key == ConsoleKey.RightArrow && (p + 1) * size < list.Count) p++;
                else if (k.Key == ConsoleKey.LeftArrow && p > 0) p--;
                else if (k.Key == ConsoleKey.Escape) break;
            }
        }
    }
    #endregion
}