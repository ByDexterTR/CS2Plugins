namespace VIPCore;

public partial class VIPCore
{
  private const string DefaultGroupsJson = """
{
  "#Lite": {
    "AdminFlags": [
      "@css/reservation",
      "@css/vip"
    ],
    "AntiFlash": {
      "self": false,
      "enemy": false,
      "teammates": true
    },
    "Armor": {
      "value": 100,
      "helmet": false
    },
    "Bhop": {
      "autostrafe": false,
      "max_speed": 350,
      "jump_boost": 1.05,
      "jump_velocity": 300
    },
    "ColoredModel": [
      "Rainbow rainbow",
      "Mavi #0000FF",
      "Yesil #00FF00"
    ],
    "DamageResist": {
      "percent": 15,
      "only_with_weapon": "",
      "ignore_teammates": true,
      "ignore_self": true
    },
    "DecoyTeleport": {
      "limit": 3
    },
    "DefuseKit": true,
    "ExtraHP": 110,
    "ExtraJump": {
      "count": 1,
      "limit": 3
    },
    "ExtraMoney": {
      "amount": 500
    },
    "ExtraSpeed": {
      "multiplier": 1.1,
      "only_with_weapon": "weapon_knife"
    },
    "FallDamage": {
      "percent": 50,
      "count": 0
    },
    "Fov": [
      50,
      60
    ],
    "GiveZeus": true,
    "HealthRegen": {
      "hp_per_tick": 3,
      "interval": 1,
      "delay_after_dmg": 5
    },
    "Healthshot": 1,
    "JoinMessage": {
      "join_message": "",
      "leave_message": ""
    },
    "PlayerGlow": {
      "range": 300,
      "team": -1,
      "colors": [
        "Rainbow rainbow",
        "Mavi #0000FF",
        "Yesil #00FF00"
      ]
    },
    "PlayerTrail": {
      "width": 1,
      "lifetime": 2,
      "colors": [
        "Rainbow rainbow",
        "Mavi #0000FF",
        "Yesil #00FF00"
      ]
    },
    "SmokeColor": [
      "Beyaz #FFFFFF",
      "Kirmizi #FF0000"
    ],
    "SpawnProtection": 2,
    "Tag": {
      "tag": "{BlueGrey}[LITE]",
      "name_color": "bluegrey",
      "chat_color": "default",
      "tab": "[LITE]"
    },
    "VIPChat": true
  },
  "#Plus": {
    "AdminFlags": [
      "@css/reservation",
      "@css/vip"
    ],
    "AntiFlash": {
      "self": true,
      "enemy": true,
      "teammates": true
    },
    "AntiHS": {
      "percent": 0,
      "only_with_weapon": ""
    },
    "Armor": {
      "value": 100,
      "helmet": true
    },
    "ArmorRegen": {
      "armor_per_tick": 10,
      "interval": 1,
      "delay_after_dmg": 2,
      "max_armor": 100,
      "give_helmet_when_full": true
    },
    "AutoHS": {
      "multiplier": 4,
      "only_with_weapon": "",
      "ignore_teammates": true
    },
    "Bhop": {
      "autostrafe": true,
      "max_speed": 500,
      "jump_boost": 1.1,
      "jump_velocity": 300
    },
    "BombsiteAnnouncer": {
      "img_a": "https://raw.githubusercontent.com/itsAudioo/CS2BombsiteAnnouncer/refs/heads/main/img/Site-A.png",
      "img_b": "https://raw.githubusercontent.com/itsAudioo/CS2BombsiteAnnouncer/refs/heads/main/img/Site-B.png",
      "duration": 5
    },
    "BulletTrail": {
      "width": 1.5,
      "lifetime": 0.6,
      "only_with_weapon": "",
      "colors": [
        "Rainbow rainbow",
        "Kirmizi #FF0000",
        "Mavi #0000FF",
        "Yesil #00FF00"
      ]
    },
    "BuyTeamWeapon": {
      "galil": true,
      "ak47": true,
      "sg553": true,
      "glock": true,
      "mac10": true,
      "tec9": true,
      "sawedoff": true,
      "g3sg1": true,
      "p2000": true,
      "mag7": true,
      "fiveseven": true,
      "famas": true,
      "m4a1": true,
      "m4a4": true,
      "aug": true,
      "scar20": true,
      "mp9": true,
      "usp": true
    },
    "ColoredModel": [
      "Rainbow rainbow",
      "Kirmizi #FF0000",
      "Yesil #00FF00",
      "Mavi #0000FF",
      "Sari #FFFF00",
      "Mor #A020F0"
    ],
    "CustomWeaponModel": [
      {
        "name": "M4A4 - AK47",
        "weapon": "weapon_m4a1",
        "model": "weapons/models/ak47/weapon_rif_ak47.vmdl"
      }
    ],
    "DamageDealt": {
      "percent": 50,
      "only_with_weapon": "",
      "ignore_teammates": true,
      "ignore_self": true
    },
    "DamageResist": {
      "percent": 40,
      "only_with_weapon": "",
      "ignore_teammates": true,
      "ignore_self": true
    },
    "DecoyTeleport": {
      "limit": 0
    },
    "DefuseKit": true,
    "ExtraHP": 150,
    "ExtraJump": {
      "count": 2,
      "limit": 0
    },
    "ExtraKillAwards": {
      "headshot": 150,
      "noscope": 100,
      "inair": 200,
      "blind": 50,
      "distance": {
        "unit": 2048,
        "money": 100
      },
      "weapon_ak47": 150,
      "weapon_knife": 1000
    },
    "ExtraMoney": {
      "amount": 4000
    },
    "ExtraSpeed": {
      "multiplier": 1.3,
      "only_with_weapon": ""
    },
    "FallDamage": {
      "percent": 0,
      "count": 0
    },
    "FastDefuse": {
      "time": 1,
      "immune_while_burning": true
    },
    "FastPlant": {
      "time": 1,
      "immune_while_burning": true
    },
    "FastReload": {
      "only_with_weapon": ""
    },
    "Fov": [
      50,
      60,
      70,
      80,
      90,
      100,
      110,
      120
    ],
    "GiveWeapon": {
      "rifle": [
        "weapon_ak47",
        "weapon_m4a1",
        "weapon_awp"
      ],
      "pistol": [
        "weapon_deagle",
        "weapon_fiveseven"
      ]
    },
    "GiveZeus": true,
    "Glaz": true,
    "GlueGrenade": {
      "only_grenades": "flashbang,hegrenade"
    },
    "Gravity": [
      1,
      0.8,
      0.5,
      0.3
    ],
    "GrenadeKit": {
      "flash": 2,
      "smoke": 1,
      "he": 3,
      "molotov": 1,
      "decoy": 0
    },
    "GrenadeResist": {
      "percent": 50,
      "only_with_grenade": "he,molotov,inferno",
      "ignore_teammates": true,
      "ignore_self": true
    },
    "GrenadeTrail": {
      "width": 1.5,
      "lifetime": 2.5,
      "colors": [
        "Rainbow rainbow",
        "Kirmizi #FF0000",
        "Mavi #0000FF",
        "Yesil #00FF00"
      ]
    },
    "HealthRegen": {
      "hp_per_tick": 10,
      "interval": 1,
      "delay_after_dmg": 2
    },
    "Healthshot": 2,
    "HitSound": [
      {
        "name": "Killcard",
        "path": "sounds/ui/killcard_1.vsnd"
      },
      {
        "name": "Back",
        "path": "sounds/ui/menu_back.vsnd"
      }
    ],
    "InfiniteAmmo": {
      "only_weapon": ""
    },
    "Invisibility": {
      "only_stopped": true,
      "dmg_after_invis": 2,
      "only_with_weapon": ""
    },
    "JoinMessage": {
      "join_message": "{Gold}{name}{Default} sunucuya katildi {Green}(PLUS VIP)",
      "leave_message": "{Gold}{name}{Default} sunucudan ayrildi"
    },
    "KillHeal": {
      "headshot": 15,
      "noscope": 10,
      "inair": 20,
      "blind": 5,
      "distance": {
        "unit": 2048,
        "hp": 10
      },
      "weapon_ak47": 15,
      "weapon_knife": 50
    },
    "KillScreen": {
      "duration": 1
    },
    "OneShot": {
      "weapons": "weapon_awp,weapon_ssg08"
    },
    "PistolRoundDisable": [
      "GiveWeapon",
      "WeaponAmmo"
    ],
    "PlayerGlow": {
      "range": 300,
      "team": -1,
      "colors": [
        "Rainbow rainbow",
        "Kirmizi #FF0000",
        "Mavi #0000FF",
        "Yesil #00FF00",
        "Sari #FFFF00",
        "Mor #A020F0"
      ]
    },
    "PlayerModel": {
      "ct": [
        {
          "name": "Special Agent Ava",
          "model": "agents/models/ctm_swat/ctm_swat_variante.vmdl",
          "arm": "",
          "leg": true
        },
        {
          "name": "Seal Team 6 Soldier",
          "model": "agents/models/ctm_st6/ctm_st6_variante.vmdl",
          "arm": "",
          "leg": true
        }
      ],
      "t": [
        {
          "name": "Sir Bloody Miami Darryl",
          "model": "agents/models/tm_professional/tm_professional_varf.vmdl",
          "arm": "",
          "leg": true
        },
        {
          "name": "Little Kev",
          "model": "agents/models/tm_professional/tm_professional_varh.vmdl",
          "arm": "",
          "leg": true
        }
      ]
    },
    "PlayerSize": [
      0.5,
      0.75,
      1.25,
      1.5
    ],
    "PlayerTrail": {
      "width": 1.5,
      "lifetime": 2.5,
      "colors": [
        "Rainbow rainbow",
        "Kirmizi #FF0000",
        "Mavi #0000FF",
        "Yesil #00FF00",
        "Sari #FFFF00",
        "Mor #A020F0"
      ]
    },
    "PoisonBullet": {
      "minhp": 10,
      "damage": 2,
      "damagetick": 1,
      "only_with_weapon": "",
      "ignore_teammates": true
    },
    "RadarHack": {
      "duration_on": 1,
      "duration_off": 0
    },
    "ReflectDamage": {
      "reflect_percent": 50,
      "max_per_shot": 100,
      "only_with_weapon": "",
      "ignore_teammates": true,
      "ignore_self": true
    },
    "Respawn": {
      "limit": 1,
      "timer": 3
    },
    "SaySound": {
      "cooldown": 2,
      "sounds": [
        {
          "name": "Beep",
          "path": "sounds/ui/beepclear.vsnd"
        },
        {
          "name": "Bep",
          "path": "sounds/ui/panorama/chatwheel_alert_01.vsnd"
        },
        {
          "name": "Bz",
          "path": "sounds/ui/panorama/radial_slideout_01.vsnd"
        },
        {
          "name": "Dong",
          "path": "sounds/ui/deathnotice.vsnd"
        }
      ]
    },
    "Silent": {
      "only_with_weapon": ""
    },
    "SmokeColor": [
      "Beyaz #FFFFFF",
      "Kirmizi #FF0000",
      "Yesil #00FF00",
      "Mavi #0000FF",
      "Sari #FFFF00",
      "Mor #A020F0"
    ],
    "SmokeEffect": {
      "poison": {
        "minhp": 10,
        "damage": 2,
        "tick": 0.5,
        "radius": 180,
        "smokecolor": [
          255,
          0,
          255
        ],
        "ignore_teammates": true,
        "ignore_self": true,
        "limit": 0
      },
      "heal": {
        "heal": 2,
        "tick": 0.5,
        "radius": 180,
        "smokecolor": [
          0,
          255,
          0
        ],
        "ignore_teammates": false,
        "ignore_self": false,
        "ignore_enemy": true,
        "limit": 0
      },
      "slow": {
        "percent": 30,
        "minspeed": 100,
        "radius": 180,
        "smokecolor": [
          0,
          0,
          255
        ],
        "ignore_teammates": true,
        "ignore_self": true,
        "ignore_enemy": false,
        "limit": 0
      }
    },
    "SpawnProtection": 4,
    "Spy": true,
    "Tag": {
      "tag": "{Gold}[{Orchid}PLUS{Gold}]",
      "name_color": "gold",
      "chat_color": "default",
      "tab": "[PLUS]"
    },
    "TeamHeal": {
      "minhp": 5,
      "percent": 50,
      "only_with_weapon": ""
    },
    "Thirdperson": {
      "distance": 120
    },
    "VIPChat": true,
    "Vampire": {
      "heal_percent": 75,
      "only_with_weapon": "",
      "max_overheal": 120,
      "ignore_teammates": true
    },
    "WeaponAmmo": [
      {
        "weapon_name": "weapon_ak47",
        "ammo": 45,
        "reserve": 5
      },
      {
        "weapon_name": "weapon_deagle",
        "ammo": 10,
        "reserve": 5
      }
    ],
    "ZeusCooldown": {
      "cooldown": 5,
      "limit": 0
    }
  }
}
""";
}
