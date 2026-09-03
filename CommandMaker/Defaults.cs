namespace CommandMaker;

public partial class CommandMaker
{
  private static void CreateDefaultCommandsJson(string path)
  {
    File.WriteAllText(path, DefaultCommandsJson);
  }

  private const string DefaultCommandsJson = """
{
  "Commands": [
    {
      "command": ["css_hp", "css_health"],
      "type": "target",
      "description": "Hedefin canını ayarlar",
      "args": 1,
      "arg1": "number",
      "arg1_number_min": 1,
      "arg1_number_max": 500,
      "arg1_default": "100",
      "flag": ["@css/slay", "@css/cheats"],
      "cooldown": 3,
      "sethealth": "[TARGET] [ARG1]",
      "chat": ["[GOLD][TARGET] [DEFAULT]adlı oyuncunun canı [GOLD][ARG1] [DEFAULT]olarak ayarlandı."],
      "targetchat": ["[GREEN]Canın [GOLD][ARG1] [GREEN]olarak ayarlandı."],
      "center": "<font color='#00ff00'>Can: [ARG1]</font>",
      "centertime": 3.0
    },
    {
      "command": ["css_maxhp"],
      "type": "target",
      "description": "Hedefin maksimum canını ayarlar",
      "args": 1,
      "arg1": "number",
      "arg1_number_min": 1,
      "arg1_number_max": 500,
      "flag": "@css/cheats",
      "setmaxhealth": "[TARGET] [ARG1]",
      "sethealth": "[TARGET] [ARG1]",
      "chat": ["[GOLD][TARGET] [DEFAULT]maksimum canı [GOLD][ARG1] [DEFAULT]oldu."]
    },
    {
      "command": ["css_zirh", "css_armor"],
      "type": "target",
      "description": "Hedefe zırh ve kask verir",
      "args": 1,
      "arg1": "number",
      "arg1_number_min": 0,
      "arg1_number_max": 100,
      "arg1_default": "100",
      "flag": "@css/cheats",
      "setarmor": "[TARGET] [ARG1]",
      "sethelmet": "[TARGET] true",
      "chat": ["[GOLD][TARGET] [DEFAULT]zırhı [GOLD][ARG1] [DEFAULT]oldu, kask verildi."],
      "targetchat": ["[GREEN]Zırhın yenilendi."]
    },
    {
      "command": ["css_ekle"],
      "type": "target",
      "description": "Hedefin canına, zırhına ve parasına ekleme yapar",
      "args": 3,
      "arg1": "number",
      "arg1_number_min": -100,
      "arg1_number_max": 100,
      "arg1_default": "25",
      "arg2": "number",
      "arg2_number_min": -100,
      "arg2_number_max": 100,
      "arg2_default": "0",
      "arg3": "number",
      "arg3_number_min": -16000,
      "arg3_number_max": 16000,
      "arg3_default": "0",
      "flag": "@css/cheats",
      "addhealth": "[TARGET] [ARG1]",
      "addarmor": "[TARGET] [ARG2]",
      "addmoney": "[TARGET] [ARG3]",
      "chat": ["[GOLD][TARGET] [DEFAULT]| Can [GOLD][ARG1] [DEFAULT]Zırh [GOLD][ARG2] [DEFAULT]Para [GOLD][ARG3] [DEFAULT]eklendi."]
    },
    {
      "command": ["css_slap"],
      "type": "target",
      "description": "Hedefe tokat atar",
      "args": 1,
      "arg1": "number",
      "arg1_number_min": 0,
      "arg1_number_max": 100,
      "arg1_default": "0",
      "flag": "@css/slay",
      "announce": true,
      "slapdamage": "[TARGET] [ARG1]",
      "screencolor": "[TARGET] 255 0 0 90 0.35 0.05",
      "emitsound": "[TARGET] Player.DamageHelmet 1.0",
      "targetcenter": "<font color='#ff4040'>Tokat yedin</font>"
    },
    {
      "command": ["css_slay", "css_kill"],
      "type": "target",
      "description": "Hedefi öldürür",
      "flag": "@css/slay",
      "announce": true,
      "kill": "[TARGET]",
      "serverchat": ["[DARKRED][TARGET] [DEFAULT]öldürüldü."]
    },
    {
      "command": ["css_respawn"],
      "type": "target",
      "description": "Ölü hedefi canlandırır",
      "flag": "@css/cheats",
      "respawn": "[TARGET]",
      "chat": ["[GOLD][TARGET] [DEFAULT]canlandırıldı."]
    },
    {
      "command": ["css_money", "css_setmoney"],
      "type": "target",
      "description": "Hedefin parasını ayarlar",
      "args": 1,
      "arg1": "number",
      "arg1_number_min": 0,
      "arg1_number_max": 65535,
      "flag": "@css/cheats",
      "setmoney": "[TARGET] [ARG1]",
      "chat": ["[GOLD][TARGET] [DEFAULT]parası [GOLD][ARG1] [DEFAULT]oldu."]
    },
    {
      "command": ["css_team", "css_changeteam"],
      "type": "target",
      "description": "Hedefi başka takıma alır",
      "args": 1,
      "arg1": "list",
      "arg1_list": "0,1,2,3",
      "flag": "@css/kick",
      "changeteam": "[TARGET] [ARG1]",
      "chat": ["[GOLD][TARGET] [DEFAULT]takımı değiştirildi. [GREY](0 yok, 1 izleyici, 2 T, 3 CT)"]
    },
    {
      "command": ["css_freeze", "css_dondur"],
      "type": "target",
      "description": "Hedefi dondurur veya çözer",
      "args": 1,
      "arg1": "list",
      "arg1_list": "true,false",
      "arg1_default": "true",
      "flag": "@css/slay",
      "setfreeze": "[TARGET] [ARG1]",
      "chat": ["[GOLD][TARGET] [DEFAULT]dondurma durumu: [GOLD][ARG1]"],
      "targetcenter": "<font color='#66ccff'>Donduruldun</font>"
    },
    {
      "command": ["css_noclip"],
      "type": "target",
      "description": "Hedefin noclip modunu açar veya kapatır",
      "flag": "@css/cheats",
      "setnoclip": "[TARGET]",
      "chat": ["[GOLD][TARGET] [DEFAULT]noclip modu değiştirildi."]
    },
    {
      "command": ["css_god"],
      "type": "target",
      "description": "Hedefe ölümsüzlük verir",
      "args": 1,
      "arg1": "list",
      "arg1_list": "true,false",
      "arg1_default": "true",
      "flag": "@css/cheats",
      "setgodmode": "[TARGET] [ARG1]",
      "chat": ["[GOLD][TARGET] [DEFAULT]ölümsüzlük: [GOLD][ARG1]"],
      "targetchat": ["[GREEN]Ölümsüzlük durumun: [GOLD][ARG1]"]
    },
    {
      "command": ["css_movetype"],
      "type": "target",
      "description": "Hedefin hareket tipini ayarlar",
      "args": 1,
      "arg1": "list",
      "arg1_list": "2,8,9,11",
      "flag": "@css/root",
      "setmovetype": "[TARGET] [ARG1]",
      "chat": ["[GOLD][TARGET] [DEFAULT]hareket tipi [GOLD][ARG1] [DEFAULT]oldu. [GREY](2 yürüme, 8 noclip, 9 uçma, 11 kapalı)"]
    },
    {
      "command": ["css_hiz", "css_speed"],
      "type": "target",
      "description": "Hedefin hızını çarpan olarak ayarlar",
      "args": 1,
      "arg1": "float",
      "arg1_number_min": 0,
      "arg1_number_max": 5,
      "arg1_default": "1.0",
      "flag": "@css/cheats",
      "setspeed": "[TARGET] [ARG1]",
      "chat": ["[GOLD][TARGET] [DEFAULT]hızı [GOLD][ARG1]x [DEFAULT]oldu. [GREY](1.0 normal)"]
    },
    {
      "command": ["css_gravity", "css_yercekimi"],
      "type": "target",
      "description": "Hedefin yerçekimini çarpan olarak ayarlar",
      "args": 1,
      "arg1": "float",
      "arg1_number_min": 0,
      "arg1_number_max": 5,
      "arg1_default": "1.0",
      "flag": "@css/cheats",
      "setgravity": "[TARGET] [ARG1]",
      "chat": ["[GOLD][TARGET] [DEFAULT]yerçekimi [GOLD][ARG1]x [DEFAULT]oldu. [GREY](1.0 normal)"]
    },
    {
      "command": ["css_getir", "css_bring"],
      "type": "target",
      "description": "Hedefi nişangahının olduğu yere getirir",
      "flag": "@css/kick",
      "teleport": "[TARGET] [PLAYERAIM]",
      "chat": ["[GOLD][TARGET] [DEFAULT]yanına getirildi."],
      "targetchat": ["[GOLD][PLAYER] [DEFAULT]seni yanına aldı."]
    },
    {
      "command": ["css_tp"],
      "type": "target",
      "description": "Hedefi verilen koordinata ışınlar",
      "args": 3,
      "arg1": "float",
      "arg1_number_min": -16384,
      "arg1_number_max": 16384,
      "arg2": "float",
      "arg2_number_min": -16384,
      "arg2_number_max": 16384,
      "arg3": "float",
      "arg3_number_min": -16384,
      "arg3_number_max": 16384,
      "flag": "@css/cheats",
      "teleport": "[TARGET] [ARG1] [ARG2] [ARG3]",
      "chat": ["[GOLD][TARGET] [DEFAULT]ışınlandı. [GREY](Konumunu öğrenmek için !ben yaz, konsola bakar)"]
    },
    {
      "command": ["css_bakdir"],
      "type": "target",
      "description": "Hedefi senin baktığın yöne çevirir",
      "flag": "@css/cheats",
      "setangle": "[TARGET] [PLAYERANGLE]",
      "chat": ["[GOLD][TARGET] [DEFAULT]senin baktığın yöne çevrildi."]
    },
    {
      "command": ["css_renk"],
      "type": "target",
      "description": "Hedefin model rengini değiştirir",
      "args": 3,
      "arg1": "number",
      "arg1_number_min": 0,
      "arg1_number_max": 255,
      "arg2": "number",
      "arg2_number_min": 0,
      "arg2_number_max": 255,
      "arg3": "number",
      "arg3_number_min": 0,
      "arg3_number_max": 255,
      "flag": "@css/cheats",
      "setplayercolor": "[TARGET] [ARG1] [ARG2] [ARG3]",
      "chat": ["[GOLD][TARGET] [DEFAULT]rengi [GOLD][ARG1] [ARG2] [ARG3] [DEFAULT]oldu."]
    },
    {
      "command": ["css_modelt"],
      "type": "target",
      "description": "Hedefe T modeli verir",
      "flag": "@css/root",
      "setmodel": "[TARGET] agents/models/tm_leet/tm_leet_varianta.vmdl",
      "chat": ["[GOLD][TARGET] [DEFAULT]modeli T oldu."]
    },
    {
      "command": ["css_modelct"],
      "type": "target",
      "description": "Hedefe CT modeli verir",
      "flag": "@css/root",
      "setmodel": "[TARGET] agents/models/ctm_sas/ctm_sas_variantf.vmdl",
      "chat": ["[GOLD][TARGET] [DEFAULT]modeli CT oldu."]
    },
    {
      "command": ["css_isim"],
      "type": "target",
      "description": "Hedefin ismini değiştirir",
      "args": 1,
      "arg1": "word",
      "arg1_word_length": 24,
      "flag": "@css/root",
      "setname": "[TARGET] [ARG1]",
      "announce": true,
      "chat": ["[GOLD][TARGET] [DEFAULT]ismi [GOLD][ARG1] [DEFAULT]oldu."]
    },
    {
      "command": ["css_tag"],
      "type": "target",
      "description": "Hedefin klan etiketini değiştirir",
      "args": 1,
      "arg1": "word",
      "arg1_word_length": 12,
      "flag": "@css/root",
      "setclantag": "[TARGET] [ARG1]",
      "chat": ["[GOLD][TARGET] [DEFAULT]etiketi [GOLD][ARG1] [DEFAULT]oldu."]
    },
    {
      "command": ["css_ver", "css_give"],
      "type": "target",
      "description": "Hedefe silah veya eşya verir",
      "args": 1,
      "arg1": "word",
      "arg1_word_length": 32,
      "flag": "@css/cheats",
      "giveweapon": "[TARGET] [ARG1]",
      "chat": ["[GOLD][TARGET] [DEFAULT]oyuncusuna [GOLD][ARG1] [DEFAULT]verildi. [GREY](ak47, item_cutters, item_kevlar)"]
    },
    {
      "command": ["css_sarjor"],
      "type": "target",
      "description": "Hedefin şarjörünü ve cephanesini doldurur",
      "flag": "@css/cheats",
      "setclip": "[TARGET] 100",
      "setammo": "[TARGET] 300",
      "chat": ["[GOLD][TARGET] [DEFAULT]şarjörü dolduruldu. [GREY](Şarjör [TARGETCLIP], Cephane [TARGETAMMO])"]
    },
    {
      "command": ["css_dusur"],
      "type": "target",
      "description": "Hedefin elindeki silahı düşürür",
      "flag": "@css/slay",
      "dropweapon": "[TARGET]",
      "chat": ["[GOLD][TARGET] [DEFAULT]silahını düşürdü."]
    },
    {
      "command": ["css_silahsil"],
      "type": "target",
      "description": "Hedefin tüm silahlarını siler",
      "flag": "@css/slay",
      "stripweapons": "[TARGET]",
      "announce": true,
      "chat": ["[GOLD][TARGET] [DEFAULT]silahları silindi."],
      "targetcenter": "<font color='#ff4040'>Silahların alındı</font>"
    },
    {
      "command": ["css_ses"],
      "type": "target",
      "description": "Hedefe ses çalar",
      "flag": "@css/generic",
      "cooldown": 10,
      "playsound": "[TARGET] sounds/ui/panorama/round_report_round_won_01.vsnd",
      "chat": ["[GOLD][TARGET] [DEFAULT]oyuncusuna ses çalındı."]
    },
    {
      "command": ["css_can"],
      "type": "playertarget",
      "description": "Canını yeniler (T, canlı)",
      "team_filter": "T",
      "alive_filter": "alive",
      "cooldown": 30,
      "uses_per_round": 2,
      "target_flag": "@css/cheats",
      "addhealth": "[TARGET] 50",
      "chat": ["[GREEN]Canın yenilendi. [DEFAULT](Can: [GOLD][PLAYERHEALTH][DEFAULT], tur başına 2 kez)"]
    },
    {
      "command": ["css_warmup"],
      "type": "execute",
      "description": "Isınma turunu başlatır",
      "flag": "@css/root",
      "no_warmup": true,
      "args": 1,
      "arg1": "number",
      "arg1_number_min": 10,
      "arg1_number_max": 300,
      "arg1_default": "60",
      "setcvar": [
        "mp_warmuptime [ARG1]",
        "mp_warmup_pausetimer 0"
      ],
      "execute": ["mp_warmup_start"],
      "serverchat": ["[GOLD][PLAYER] [DEFAULT]ısınma turunu başlattı. [GOLD]([ARG1] sn)"]
    },
    {
      "command": ["css_bitir"],
      "type": "execute",
      "description": "Isınma turunu bitirir",
      "flag": "@css/root",
      "warmup_only": true,
      "execute": ["mp_warmup_end"],
      "serverchat": ["[GOLD][PLAYER] [DEFAULT]ısınma turunu bitirdi."]
    },
    {
      "command": ["css_duyuru"],
      "type": "default",
      "description": "Tüm sunucuya duyuru yapar",
      "flag": "@css/chat",
      "global_cooldown": 30,
      "min_players": 2,
      "args": 1,
      "arg1": "word",
      "arg1_word_length": 64,
      "serverchat": ["[ORCHID]DUYURU [DEFAULT]| [GOLD][PLAYER][DEFAULT]: [ARG1]"],
      "servercenter": "<font class='fontSize-m' color='#ffd700'>[ARG1]</font>",
      "centertime": 5.0
    },
    {
      "command": ["css_site"],
      "type": "default",
      "description": "Sunucu bağlantılarını gösterir",
      "chat": [
        "[GOLD]Web Sitemiz: [DEFAULT]https://bydexter.net/",
        "[GOLD]Discord: [DEFAULT]discord.gg/bydexter"
      ]
    },
    {
      "command": ["css_serverinfo"],
      "type": "default",
      "description": "Sunucu bilgilerini gösterir",
      "cooldown": 5,
      "chat": [
        "[ORCHID]Sunucu: [GOLD][HOSTNAME] [DEFAULT]| IP: [GOLD][SERVERIP]:[SERVERPORT] [DEFAULT]| Harita: [GOLD][MAPNAME]",
        "[ORCHID]Saat: [GOLD][TIME] [DEFAULT]| Tarih: [GOLD][DATE] [DEFAULT]| Raunt: [GOLD][ROUND] [DEFAULT]| Kalan: [GOLD][TIMELEFT] sn [DEFAULT]| Isınma: [GOLD][WARMUP]",
        "[ORCHID]Skor: [GOLD]CT [CTSCORE] [DEFAULT]- [GOLD][TSCORE] T [DEFAULT]| Slot: [GOLD][PLAYERCOUNT]/[MAXPLAYERS] [DEFAULT]| Bot: [GOLD][BOTCOUNT]",
        "[ORCHID]Oyuncu: [DEFAULT]T [GOLD][TCOUNT] [DEFAULT]CT [GOLD][CTCOUNT] [DEFAULT]İzleyici [GOLD][SPECCOUNT]",
        "[ORCHID]Canlı: [GOLD][ALIVECOUNT] [DEFAULT](T [GOLD][ALIVET] [DEFAULT]CT [GOLD][ALIVECT][DEFAULT]) | Ölü: [GOLD][DEADCOUNT] [DEFAULT](T [GOLD][DEADT] [DEFAULT]CT [GOLD][DEADCT][DEFAULT])"
      ]
    },
    {
      "command": ["css_my", "css_ben"],
      "type": "default",
      "description": "Kendi bilgilerini gösterir",
      "chat": [
        "[GOLD][PLAYER] [DEFAULT]| Can [GOLD][PLAYERHEALTH] [DEFAULT]Zırh [GOLD][PLAYERARMOR] [DEFAULT]Para [GOLD][PLAYERMONEY] [DEFAULT]Takım [GOLD][PLAYERTEAM]",
        "[ORCHID]Silah: [GOLD][PLAYERWEAPON] [DEFAULT]| Şarjör [GOLD][PLAYERCLIP] [DEFAULT]Cephane [GOLD][PLAYERAMMO]",
        "[ORCHID]Skor: [GOLD][PLAYERSCORE] [DEFAULT]| Öldürme [GOLD][PLAYERKILLS] [DEFAULT]Ölüm [GOLD][PLAYERDEATHS] [DEFAULT]Asist [GOLD][PLAYERASSISTS] [DEFAULT]K/D [GOLD][PLAYERKDR]",
        "[ORCHID]Ping: [GOLD][PLAYERPING] [DEFAULT]| Etiket: [GOLD][PLAYERCLAN] [DEFAULT]| Baktığın: [GOLD][PLAYERAIMTARGET]"
      ],
      "console": [
        "SteamID64: [PLAYERSTEAMID] | UserID: [PLAYERUSERID]",
        "Konum: [PLAYERCOORDINATE] | Açı: [PLAYERANGLE] | Nişangah: [PLAYERAIM]",
        "Sunucu: [HOSTNAME] | Harita: [MAPNAME]"
      ]
    },
    {
      "command": ["css_target", "css_hedef"],
      "type": "target",
      "description": "Bir oyuncunun bilgilerini gösterir",
      "flag": "@css/generic",
      "ignore_immunity": true,
      "chat": [
        "[GOLD][TARGET] [DEFAULT]| Can [GOLD][TARGETHEALTH] [DEFAULT]Zırh [GOLD][TARGETARMOR] [DEFAULT]Para [GOLD][TARGETMONEY] [DEFAULT]Takım [GOLD][TARGETTEAM]",
        "[ORCHID]Silah: [GOLD][TARGETWEAPON] [DEFAULT]| Şarjör [GOLD][TARGETCLIP] [DEFAULT]Cephane [GOLD][TARGETAMMO]",
        "[ORCHID]Skor: [GOLD][TARGETSCORE] [DEFAULT]| Öldürme [GOLD][TARGETKILLS] [DEFAULT]Ölüm [GOLD][TARGETDEATHS] [DEFAULT]Asist [GOLD][TARGETASSISTS] [DEFAULT]K/D [GOLD][TARGETKDR]",
        "[ORCHID]Ping: [GOLD][TARGETPING] [DEFAULT]| Etiket: [GOLD][TARGETCLAN] [DEFAULT]| Uzaklık: [GOLD][TARGETDISTANCE]"
      ],
      "console": [
        "Hedef SteamID64: [TARGETSTEAMID] | UserID: [TARGETUSERID]",
        "Hedef konum: [TARGETCOORDINATE] | Açı: [TARGETANGLE]"
      ]
    },
    {
      "command": ["css_rastgele"],
      "type": "default",
      "description": "Rastgele oyuncu ve sayı seçer",
      "flag": "@css/chat",
      "cooldown": 10,
      "chat": [
        "[ORCHID]Rastgele: [GOLD][RANDOMPLAYER] [DEFAULT]| T: [GOLD][RANDOMT] [DEFAULT]| CT: [GOLD][RANDOMCT]",
        "[ORCHID]Canlı: [GOLD][RANDOMALIVE] [DEFAULT]| Ölü: [GOLD][RANDOMDEAD]",
        "[ORCHID]Canlı T: [GOLD][RANDOMTALIVE] [DEFAULT]| Ölü T: [GOLD][RANDOMTDEAD]",
        "[ORCHID]Canlı CT: [GOLD][RANDOMCTALIVE] [DEFAULT]| Ölü CT: [GOLD][RANDOMCTDEAD]",
        "[ORCHID]Zar: [GOLD][RANDOM:1-6] [DEFAULT]| Yüzde: [GOLD][RANDOM:1-100]"
      ]
    },
    {
      "command": ["css_kick"],
      "type": "target",
      "description": "Hedefi sunucudan atar",
      "flag": "@css/kick",
      "announce": true,
      "execute": ["kickid [TARGETUSERID] Yetkili tarafindan atildin"],
      "serverchat": ["[DARKRED][TARGET] [DEFAULT]sunucudan atıldı."]
    },
    {
      "command": ["css_menu", "css_adminmenu"],
      "type": "menu",
      "description": "Yetkili menüsünü açar",
      "flag": "@css/generic",
      "menu_title": "[GOLD]Yetkili Menüsü",
      "menu": [
        { "text": "Bilgilerim", "command": "css_my" },
        { "text": "Sunucu bilgisi", "command": "css_serverinfo" },
        { "text": "Rastgele seç", "command": "css_rastgele", "flag": "@css/chat" },
        { "text": "Baktığımı getir", "command": "css_getir @aim", "flag": "@css/kick" },
        { "text": "Baktığıma tokat", "command": "css_slap @aim 5", "flag": "@css/slay" },
        { "text": "Baktığını iyileştir", "command": "css_hp @aim 100", "flag": "@css/slay" },
        { "text": "Herkese zırh", "command": "css_zirh @all 100", "flag": "@css/cheats", "close": false },
        { "text": "Isınmayı başlat", "command": "css_warmup 60", "flag": "@css/root" }
      ]
    }
  ]
}
""";
}
