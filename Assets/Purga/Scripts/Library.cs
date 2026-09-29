using System.Collections.Generic;
using UnityEngine;

namespace Purga
{
    /// <summary>
    /// Milestone 1 data, built in code straight from the design doc numbers.
    /// Migrate to .asset files (ScriptableObjects in the Project window) in Milestone 2.
    /// </summary>
    public static class Library
    {
        // ---------------- HEROES ----------------

        public static UnitDef Veteran()
        {
            return UnitDef.New("Fusilero", u =>
            {
                u.team = Team.Heroes;
                u.maxHP = 27; u.armor = 2; u.dodge = 8; u.speed = 4;
                u.corrResist = 30; u.maxAmmo = 6;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Descarga de fusil", a => {
                        a.usableFrom = new[]{2,3}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 90; a.dmgMin = 5; a.dmgMax = 9; a.critPct = 5;
                        a.ammoCost = 1; a.blockedInMelee = true;
                    }),
                    AbilityDef.New("Calar la bayoneta", a => {
                        a.targetKind = TargetKind.Self; a.accuracy = 0;
                        a.usesPerCombat = 1; a.special = SpecialKind.ToggleMelee;
                    }),
                    AbilityDef.New("Estocada", a => {
                        a.usableFrom = new[]{1,2}; a.targetPos = new[]{1,2};
                        a.accuracy = 95; a.dmgMin = 6; a.dmgMax = 10; a.critPct = 8;
                        a.requiresMelee = true;
                    }),
                    AbilityDef.New("Fuego de contención", a => {
                        a.usableFrom = new[]{3,4}; a.targetPos = new[]{2,3};
                        a.accuracy = 85; a.dmgMin = 2; a.dmgMax = 4; a.critPct = 0;
                        a.ammoCost = 2; a.blockedInMelee = true;
                        a.status = new StatusApply{ kind = StatusKind.AccDebuff, chance = 100, power = 10, duration = 2 };
                    }),
                    AbilityDef.New("Granada de trinchera", a => {
                        a.usableFrom = new[]{2,3}; a.targetPos = new[]{2,3}; a.area = true;
                        a.accuracy = 80; a.dmgMin = 4; a.dmgMax = 7; a.critPct = 5;
                        a.usesPerCombat = 1;
                    }),
                    AbilityDef.New("\u00a1Aguantad la l\u00ednea!", a => {
                        a.targetKind = TargetKind.AllAllies; a.accuracy = 0;
                        a.usesPerCombat = 1; a.selfFaithDelta = 2;
                        a.status = new StatusApply{ kind = StatusKind.AccBuff, chance = 100, power = 10, duration = 1 };
                    }),
                    AbilityDef.New("Cuerpo a tierra", a => {
                        a.targetKind = TargetKind.Self; a.accuracy = 0;
                        a.special = SpecialKind.GuardLowest;
                    }),
                };
            });
        }

        public static UnitDef Preacher()
        {
            return UnitDef.New("Capellán", u =>
            {
                u.team = Team.Heroes;
                u.maxHP = 25; u.armor = 1; u.dodge = 5; u.speed = 3;
                u.corrResist = 45; u.maxAmmo = 2; u.usesPromethium = true; // frascos de promethium
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Pala de zapa", a => {
                        a.usableFrom = new[]{1,2}; a.targetPos = new[]{1,2};
                        a.accuracy = 85; a.dmgMin = 7; a.dmgMax = 12; a.critPct = 8;
                        // huge weapon: -2 speed on own next initiative (applied to self on use)
                    }),
                    AbilityDef.New("Sermón de la Llama", a => {
                        // Flare over the front inverted the light axis: the Sermón is no longer rear-only
                        // (the rear is dark now). Usable from anywhere so the Capellán can stand in the lit
                        // FRONT for the ×1.3 lit-miracle bonus, or hang back in shadow and forgo it.
                        a.usableFrom = new[]{1,2,3,4}; a.targetKind = TargetKind.AllAllies;
                        a.accuracy = 0; a.corruptionDelta = -8; a.isFaithAbility = true;
                    }),
                    AbilityDef.New("Absolución", a => {
                        a.targetKind = TargetKind.Ally; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 0; a.corruptionDelta = -20; a.usesPerCombat = 2;
                        a.faithDelta = 1; a.isFaithAbility = true;
                    }),
                    AbilityDef.New("Vendas y rezos", a => {
                        a.targetKind = TargetKind.Ally; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 0; a.healMin = 4; a.healMax = 6; a.removesBleed = true;
                    }),
                    AbilityDef.New("\u00a1Fuego de trinchera!", a => {
                        a.usableFrom = new[]{3,4}; a.targetPos = new[]{2,3};
                        a.accuracy = 85; a.dmgMin = 3; a.dmgMax = 5; a.critPct = 0;
                        a.ammoCost = 1; a.isFire = true;
                        a.status = new StatusApply{ kind = StatusKind.Burn, chance = 100, power = 2, duration = 3 };
                    }),
                    AbilityDef.New("Letanía del Odio", a => {
                        a.targetKind = TargetKind.AllAllies; a.accuracy = 0;
                        a.status = new StatusApply{ kind = StatusKind.DmgBuffPct, chance = 100, power = 15, duration = 3 };
                    }),
                    // "Martirio" is a passive: postponed to Milestone 2 (needs the passive system).
                };
            });
        }

        public static UnitDef Sister()
        {
            return UnitDef.New("Sanadora", u =>
            {
                u.team = Team.Heroes;
                u.maxHP = 24; u.armor = 3; u.dodge = 10; u.speed = 5;
                u.corrResist = 50; u.maxAmmo = 4; // bolter magazine: capacity is OUR number (slice says "munición" without ammount)
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Espada de la Vigilia", a => {
                        a.usableFrom = new[]{1,2}; a.targetPos = new[]{1,2};
                        a.accuracy = 90; a.dmgMin = 6; a.dmgMax = 11; a.critPct = 10;
                    }),
                    AbilityDef.New("Pistola pesada", a => {
                        a.usableFrom = new[]{2,3}; a.targetPos = new[]{2,3};
                        a.accuracy = 85; a.dmgMin = 4; a.dmgMax = 8; a.critPct = 6;
                        a.ammoCost = 1;
                    }),
                    AbilityDef.New("Llama ardiente", a => {
                        a.usableFrom = new[]{1}; a.targetPos = new[]{1,2}; a.area = true;
                        a.accuracy = 85; a.dmgMin = 5; a.dmgMax = 8; a.critPct = 5;
                        a.isFire = true;
                        a.status = new StatusApply{ kind = StatusKind.Burn, chance = 100, power = 2, duration = 3 };
                    }),
                    AbilityDef.New("Milagro: Escudo de la Llama", a => {
                        a.targetKind = TargetKind.AllAllies; a.accuracy = 0;
                        a.faithCost = 2; a.isFaithAbility = true;
                        a.status = new StatusApply{ kind = StatusKind.DmgResistPct, chance = 100, power = 30, duration = 2 };
                    }),
                    AbilityDef.New("Milagro: Condena", a => {
                        a.targetKind = TargetKind.Self; a.accuracy = 0;
                        a.faithCost = 3; a.isFaithAbility = true;
                        a.status = new StatusApply{ kind = StatusKind.JudgmentNext, chance = 100, power = 0, duration = 99 };
                    }),
                    AbilityDef.New("Himno de guerra", a => {
                        a.targetKind = TargetKind.Self; a.accuracy = 0;
                        a.special = SpecialKind.Himno; a.isFaithAbility = true;
                    }),
                    AbilityDef.New("Desafío", a => {
                        a.usableFrom = new[]{1}; a.targetKind = TargetKind.Self; a.accuracy = 0;
                        a.status = new StatusApply{ kind = StatusKind.Taunt, chance = 100, power = 0, duration = 1 };
                        // +10 dodge while taunting is granted alongside in resolution (see UseAbility)
                    }),
                };
            });
        }

        public static UnitDef Overseer()
        {
            return UnitDef.New("Oficial", u =>
            {
                u.team = Team.Heroes;
                u.maxHP = 22; u.armor = 1; u.dodge = 12; u.speed = 6;
                u.corrResist = 40; u.maxAmmo = 6; // pistol magazine: capacity is OUR number
                u.isLeader = true;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Revólver de oficial", a => {
                        a.usableFrom = new[]{2,3}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 88; a.dmgMin = 4; a.dmgMax = 8; a.critPct = 8;
                        a.ammoCost = 1;
                    }),
                    AbilityDef.New("Sable de oficial", a => {
                        a.usableFrom = new[]{1,2}; a.targetPos = new[]{1,2};
                        a.accuracy = 90; a.dmgMin = 5; a.dmgMax = 9; a.critPct = 6;
                    }),
                    AbilityDef.New("¡Ni un paso atrás!", a => {
                        a.targetKind = TargetKind.AllAllies; a.accuracy = 0;
                        // push immunity pending push-on-heroes mechanics; the +10% corr resist works now
                        a.status = new StatusApply{ kind = StatusKind.CorrResistMod, chance = 100, power = 10, duration = 2 };
                    }),
                    AbilityDef.New("Arenga férrea", a => {
                        a.targetKind = TargetKind.Ally; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 0; a.corruptionDelta = -10; a.healMin = 2; a.healMax = 2;
                    }),
                    AbilityDef.New("Señalar al renegado", a => {
                        a.targetKind = TargetKind.Enemy; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 0; // pointing cannot miss
                        a.status = new StatusApply{ kind = StatusKind.Marked, chance = 100, power = 20, duration = 2 };
                    }),
                    AbilityDef.New("Disparo de advertencia", a => {
                        a.usableFrom = new[]{2,3}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 95; a.dmgMin = 1; a.dmgMax = 2; a.critPct = 0;
                        a.ammoCost = 1;
                        a.status = new StatusApply{ kind = StatusKind.Stun, chance = 110, power = 0, duration = 1 };
                    }),
                    AbilityDef.New("FUSILAR", a => {
                        a.targetKind = TargetKind.Ally; a.accuracy = 0;
                        a.special = SpecialKind.Ejecucion;
                    }),
                };
            });
        }

        public static UnitDef Listener()
        {
            return UnitDef.New("Vidente", u =>
            {
                u.team = Team.Heroes;
                u.maxHP = 17; u.armor = 0; u.dodge = 6; u.speed = 7;
                u.corrResist = 20;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Toque vidente", a => {
                        a.usableFrom = new[]{3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 90; a.dmgMin = 3; a.dmgMax = 6; a.critPct = 5;
                        a.ignoresArmor = true; // his "safe" attack: no Peril roll
                    }),
                    AbilityDef.New("Lanza del Foso", a => {
                        a.usableFrom = new[]{3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 85; a.dmgMin = 8; a.dmgMax = 14; a.critPct = 12;
                        a.ignoresArmor = true; a.isFire = true; a.warpDanger = true;
                    }),
                    AbilityDef.New("Aplastamiento mental", a => {
                        a.usableFrom = new[]{3,4}; a.targetPos = new[]{3,4};
                        a.accuracy = 85; a.dmgMin = 4; a.dmgMax = 7; a.critPct = 5;
                        a.ignoresArmor = true; a.warpDanger = true;
                        a.cohesionDelta = 5; // psychic terror: OUR number for "+15 Corr al enemigo" vs Cohesion
                    }),
                    AbilityDef.New("Barrera cinética", a => {
                        a.targetKind = TargetKind.Ally; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 0; a.warpDanger = true;
                        a.status = new StatusApply{ kind = StatusKind.ArmorBuff, chance = 100, power = 3, duration = 3 };
                    }),
                    AbilityDef.New("Premonición", a => {
                        a.targetKind = TargetKind.AllAllies; a.accuracy = 0; a.warpDanger = true;
                        a.status = new StatusApply{ kind = StatusKind.DodgeBuff, chance = 100, power = 10, duration = 2 };
                    }),
                    AbilityDef.New("Grito del Foso", a => {
                        a.usableFrom = new[]{3,4}; a.targetKind = TargetKind.AllEnemies; a.area = true;
                        a.accuracy = 80; a.dmgMin = 3; a.dmgMax = 5; a.critPct = 0;
                        a.ignoresArmor = true; a.warpDanger = true; a.pushesBack = true;
                    }),
                    AbilityDef.New("Contención", a => {
                        a.targetKind = TargetKind.Self; a.accuracy = 0; a.corruptionDelta = -15;
                        a.status = new StatusApply{ kind = StatusKind.Stun, chance = 100, power = 0, duration = 1 }; // loses next turn
                    }),
                };
            });
        }

        /// <summary>Autómata (clase-firma, Hito 6): a war machine. Immune to la Mancha, never gains la Llama,
        /// no Milagro benefit, no bonds. A durable frontliner that breaks (KO) instead of dying (6b).</summary>
        public static UnitDef Automaton()
        {
            return UnitDef.New("Autómata", u =>
            {
                u.team = Team.Heroes;
                u.maxHP = 30; u.armor = 3; u.dodge = 4; u.speed = 3;
                u.corrResist = 100; u.maxAmmo = 0; // ResMancha inmune (moot: isAutomaton gates it); sin munición
                u.isAutomaton = true;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Puño hidráulico", a => {
                        a.usableFrom = new[]{1,2}; a.targetPos = new[]{1,2};
                        a.accuracy = 90; a.dmgMin = 8; a.dmgMax = 13; a.critPct = 6;
                    }),
                    AbilityDef.New("Ariete", a => {
                        a.usableFrom = new[]{1,2}; a.targetPos = new[]{1,2,3};
                        a.accuracy = 85; a.dmgMin = 5; a.dmgMax = 9; a.critPct = 5; a.usesPerCombat = 2;
                        a.status = new StatusApply{ kind = StatusKind.Stun, chance = 70, power = 0, duration = 1 };
                    }),
                    AbilityDef.New("Descarga de vapor", a => {
                        a.usableFrom = new[]{1,2,3}; a.targetPos = new[]{1,2}; a.area = true;
                        a.accuracy = 80; a.dmgMin = 4; a.dmgMax = 7; a.critPct = 0; a.usesPerCombat = 1;
                    }),
                    AbilityDef.New("Blindar", a => {
                        a.targetKind = TargetKind.Self; a.accuracy = 0;
                        a.status = new StatusApply{ kind = StatusKind.ArmorBuff, chance = 100, power = 3, duration = 2 };
                    }),
                    AbilityDef.New("Anclar", a => {
                        a.targetKind = TargetKind.Self; a.accuracy = 0;
                        a.special = SpecialKind.GuardLowest; // a wall of iron: covers the weakest of the squad
                    }),
                };
            });
        }

        /// <summary>Demon spawned by the Psyker's Perils fumble (Intrusión). Not part of encounters.</summary>
        public static UnitDef Larva()
        {
            return UnitDef.New("Descendido Menor", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 8; u.armor = 0; u.dodge = 12; u.speed = 8;
                u.countsForCohesion = false; // demons don't doubt
                u.threatChannel = ThreatChannel.Foso; // a Descendido reaches only into the dark
                u.isDescendido = true;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Zarpazo", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 75; a.dmgMin = 3; a.dmgMax = 5; a.critPct = 0;
                        a.corruptionDelta = 5;
                    }),
                };
            });
        }

        // ---------------- EXPEDITION ITEMS (slice §5.1) ----------------
        // Deferred until their systems exist: Agua bendita (curiosidades 3.4),
        // Ganzúas y Pala (salas cerradas/escombros 3.4), Baliza vox (Vox 3.6).

        public static ItemDef Cargador() => ItemDef.New("Cargador de fusil ×6", i => {
            i.price = 75; i.charges = 6; i.kind = ItemKind.AmmoRefill;
            i.description = "6 recargas de armas de fuego. Se consumen solas entre combates.";
        });

        public static ItemDef Promethium() => ItemDef.New("Frasco de fuego de trinchera", i => {
            i.price = 100; i.charges = 2; i.kind = ItemKind.Promethium;
            i.description = "Rellena los frascos del Capellán (2 usos). También quema nidos y biomasa.";
        });

        public static ItemDef Racion() => ItemDef.New("Ración de campaña", i => {
            i.price = 60; i.charges = 1; i.kind = ItemKind.Ration; i.power = 3;
            i.description = "Cura 3 PV fuera de combate.";
        });

        public static ItemDef KitMedico() => ItemDef.New("Kit médico", i => {
            i.price = 90; i.charges = 1; i.kind = ItemKind.MedKit; i.power = 4;
            i.description = "Cura 4 PV y elimina Sangrado y Gas fuera de combate.";
        });

        public static ItemDef Sello() => ItemDef.New("Escapulario", i => {
            i.price = 200; i.charges = 1; i.kind = ItemKind.PuritySeal; i.power = 25;
            i.description = "−25 Mancha a un personaje. Uso único. Caro adrede.";
        });

        public static ItemDef Servocraneo() => ItemDef.New("el Farol médico", i => {
            i.price = 150; i.charges = 999; i.kind = ItemKind.ServoSkull; i.power = 2;
            i.description = "Sigue al grupo: cura 2 PV al más herido tras cada combate.";
        });

        public static ItemDef Botin() => ItemDef.New("Botín de la Congregación ★", i => {
            i.price = 0; i.charges = 1; i.kind = ItemKind.Loot;
            i.description = "Reliquias y objetos de valor. Ocupan hueco; se venden al volver.";
        });

        public static ItemDef AguaBendita() => ItemDef.New("Agua bendita", i => {
            i.price = 90; i.charges = 1; i.kind = ItemKind.HolyWater;
            i.description = "Consagra curiosidades profanadas.";
        });

        public static ItemDef Ganzuas() => ItemDef.New("Ganzúas", i => {
            i.price = 50; i.charges = 2; i.kind = ItemKind.Lockpicks;
            i.description = "Abren relicarios y cofres sellados (2 usos).";
        });

        public static ItemDef KitCampamento() => ItemDef.New("Kit de refugio", i => {
            i.price = 100; i.charges = 1; i.kind = ItemKind.CampKit;
            i.description = "Permite refugiarse en un Refugio: el grupo descansa y se serena.";
        });

        public static ItemDef Baliza() => ItemDef.New("Repetidor de señal", i => {
            i.price = 120; i.charges = 1; i.kind = ItemKind.VoxBeacon;
            i.description = "+2 niveles de Señal con el Fortín al desplegarlo (más seguro, evac, menos botín).";
        });

        public static ItemDef MascaraGas() => ItemDef.New("máscara de gas", i => {
            i.price = 60; i.charges = 999; i.kind = ItemKind.GasMask;
            i.description = "Suministro reutilizable: mientras la lleváis, la Nube de Gas ambiental no os afecta. No filtra el Gas de las armas enemigas.";
        });

        // ---------------- EQUIPO (el Barracón) ----------------
        // Gear MODIFIES on top of class base + Taller rank (decided). Weapons/armour are net-positive
        // sidegrades; trinkets (collar/anillo) are pro/con: a real benefit paid with a real cost.

        // -- Armas --
        public static GearDef FusilLargo() => GearDef.New("Fusil de cerrojo largo", GearSlot.Weapon, g => {
            g.dmgBonus = 2; g.speedBonus = -1; g.price = 350;
            g.flavor = "Cañón alargado: pega más fuerte, pero se maneja despacio.";
        });
        public static GearDef Recortada() => GearDef.New("Recortada de trinchera", GearSlot.Weapon, g => {
            g.dmgBonus = 3; g.accBonus = -5; g.price = 300;
            g.flavor = "Escupe plomo a bocajarro. Puntería, poca.";
        });
        public static GearDef BayonetaAfilada() => GearDef.New("Bayoneta afilada", GearSlot.Weapon, g => {
            g.dmgBonus = 1; g.accBonus = 5; g.price = 250;
            g.flavor = "Acero bien templado, mano firme.";
        });
        public static GearDef RevolverRepeticion() => GearDef.New("Revólver de repetición", GearSlot.Weapon, g => {
            g.dmgBonus = 1; g.speedBonus = 1; g.price = 300;
            g.flavor = "Ligero y rápido en el cinto del oficial.";
        });

        // -- Armaduras --
        public static GearDef CorazaAsalto() => GearDef.New("Coraza de asalto", GearSlot.Armor, g => {
            g.armorBonus = 2; g.speedBonus = -1; g.price = 400;
            g.flavor = "Chapa gruesa de parapeto. Aguanta, pero pesa.";
        });
        public static GearDef PetoLigero() => GearDef.New("Peto ligero", GearSlot.Armor, g => {
            g.dodgeBonus = 1; g.speedBonus = 1; g.price = 300;
            g.flavor = "Cuero y remaches: te mueves, no te blindas.";
        });
        public static GearDef PlacasTrinchera() => GearDef.New("Placas de trinchera", GearSlot.Armor, g => {
            g.armorBonus = 2; g.dodgeBonus = -5; g.price = 350;
            g.flavor = "Placas atornilladas. Ni una esquirla te entra; ni una esquiva te sale.";
        });

        // -- Collares (pro/contra) --
        public static GearDef MedallaVigilia() => GearDef.New("Medalla de la Vigilia", GearSlot.Neck, g => {
            g.manchaResBonus = 10; g.dmgBonus = -1; g.price = 250;
            g.flavor = "La fe blinda el alma y ablanda el brazo.";
        });
        public static GearDef ColmilloFoso() => GearDef.New("Colmillo del Foso", GearSlot.Neck, g => {
            g.dmgBonus = 2; g.manchaResBonus = -8; g.price = 0;
            g.flavor = "Trofeo de un Descendido. Da rabia y cobra su precio.";
        });
        public static GearDef RosarioLaton() => GearDef.New("Rosario de latón", GearSlot.Neck, g => {
            g.speedBonus = 2; g.hpBonus = -2; g.price = 0;
            g.flavor = "Reza deprisa quien poco vive.";
        });

        // -- Anillos (pro/contra) --
        public static GearDef AnilloOficial() => GearDef.New("Anillo del oficial", GearSlot.Ring, g => {
            g.accBonus = 5; g.dodgeBonus = -5; g.price = 250;
            g.flavor = "Apunta con frialdad quien no piensa esquivar.";
        });
        public static GearDef SelloPlomo() => GearDef.New("Sello de plomo", GearSlot.Ring, g => {
            g.armorBonus = 1; g.speedBonus = -1; g.price = 0;
            g.flavor = "Un lastre sagrado sobre el nudillo.";
        });
        public static GearDef AnilloHerida() => GearDef.New("Anillo de la Herida", GearSlot.Ring, g => {
            g.dmgBonus = 2; g.manchaResBonus = -10; g.price = 0;
            g.flavor = "Late. No preguntes por qué.";
        });

        // ---------------- CONSUMABLES (slice §5.2) ----------------

        public static ConsumableDef Recaf() => ConsumableDef.New("ración de recaf", c => {
            c.price = 30;
            c.status = new StatusApply{ kind = StatusKind.SpeedBuff, chance = 100, power = 2, duration = 3 };
        });

        public static ConsumableDef Amasec() => ConsumableDef.New("aguardiente de trinchera", c => {
            c.price = 45; c.corruptionDelta = -10;
            c.status = new StatusApply{ kind = StatusKind.AccDebuff, chance = 100, power = 5, duration = 2 };
        });

        public static ConsumableDef Estimulante() => ConsumableDef.New("inyector de combate", c => {
            c.price = 80;
            c.status = new StatusApply{ kind = StatusKind.DmgBuffPct, chance = 100, power = 25, duration = 3 };
            c.statusOnExpire = new StatusApply{ kind = StatusKind.CorrResistMod, chance = 100, power = -10, duration = 99 };
        });

        public static ConsumableDef Contraveneno() => ConsumableDef.New("antídoto de gas", c => {
            c.price = 40; c.removesToxin = true; c.toxinImmuneRounds = 3;
        });

        public static ConsumableDef Incienso() => ConsumableDef.New("Incienso", c => {
            c.price = 70; c.affectsGroup = true;
            c.status = new StatusApply{ kind = StatusKind.CorrResistMod, chance = 100, power = 15, duration = 3 };
        });

        public static ConsumableDef Coraje() => ConsumableDef.New("valor de petaca", c => {
            c.price = 60; c.removesQuebranto = true; c.endCombatCorruption = 15;
        });

        // ---------------- ENEMIES (Stage 1) ----------------

        public static UnitDef Cultist()
        {
            return UnitDef.New("Renegado", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 8; u.armor = 0; u.dodge = 5; u.speed = 3; u.threat = 1;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Cuchillo de zanja", a => {
                        a.usableFrom = new[]{1,2}; a.targetPos = new[]{1,2};
                        a.accuracy = 70; a.dmgMin = 2; a.dmgMax = 4; a.critPct = 3;
                    }),
                    AbilityDef.New("Pistola", a => {
                        a.usableFrom = new[]{3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 70; a.dmgMin = 2; a.dmgMax = 5; a.critPct = 3;
                    }),
                };
            });
        }

        public static UnitDef Neophyte()
        {
            return UnitDef.New("Renegado con escopeta", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 12; u.armor = 0; u.dodge = 8; u.speed = 4; u.threat = 2;
                u.maxAmmo = 2; // shotgun: 2 shells then a reload round (the punish window)
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Escopeta", a => {
                        a.usableFrom = new[]{1,2,3}; a.targetPos = new[]{1,2};
                        a.accuracy = 75; a.dmgMin = 4; a.dmgMax = 7; a.critPct = 5;
                        a.ammoCost = 1;
                    }),
                };
            });
        }

        public static UnitDef Whisperer()
        {
            return UnitDef.New("Predicador de la Herida", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 10; u.armor = 0; u.dodge = 10; u.speed = 5; u.threat = 2;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Prédica de la Herida", a => {
                        a.targetKind = TargetKind.Enemy; a.accuracy = 0;
                        a.special = SpecialKind.Cantico; // +8 corruption to 2 random heroes
                        a.corruptionDelta = 8;
                    }),
                };
            });
        }

        public static UnitDef Aberrant()
        {
            return UnitDef.New("Tocado deforme", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 30; u.armor = 2; u.dodge = 2; u.speed = 2; u.threat = 4;
                u.countsForCohesion = false; // monsters don't doubt
                u.weakToFire = true;         // fire x1.5, Burn DoT x2
                u.threatChannel = ThreatChannel.Foso; // Foso flesh: it grabs what the light has abandoned
                u.resStun = 40; u.resMove = 50; // massive frame (OUR numbers, editable in the asset)
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Garrote de hueso", a => {
                        a.usableFrom = new[]{1,2}; a.targetPos = new[]{1,2};
                        a.accuracy = 75; a.dmgMin = 7; a.dmgMax = 12; a.critPct = 5;
                        a.status = new StatusApply{ kind = StatusKind.Stun, chance = 25, power = 0, duration = 1 };
                    }),
                };
            });
        }

        // ---------------- HUMANOS: bestiary §9.1 (Sectores 1-2, tienen Moral) ----------------

        // el Iluminado: buffs the cult's Moral instead of fighting.
        public static UnitDef Zealot()
        {
            return UnitDef.New("Iluminado", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 11; u.armor = 0; u.dodge = 8; u.speed = 5; u.threat = 3;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Letanía inflamada", a => {
                        a.targetKind = TargetKind.Self; a.accuracy = 0; a.ralliesMoral = 8;
                    }),
                    AbilityDef.New("Estaca", a => {
                        a.usableFrom = new[]{1,2}; a.targetPos = new[]{1,2};
                        a.accuracy = 70; a.dmgMin = 2; a.dmgMax = 4; a.critPct = 3;
                    }),
                };
            });
        }

        // el Bombardero: telegraphed area attack (prime one turn, detonate on the front line the next).
        public static UnitDef Bomber()
        {
            return UnitDef.New("Bombardero", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 13; u.armor = 0; u.dodge = 6; u.speed = 4; u.threat = 3;
                u.isBomber = true;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Culatazo", a => {
                        a.usableFrom = new[]{1,2}; a.targetPos = new[]{1,2};
                        a.accuracy = 65; a.dmgMin = 2; a.dmgMax = 3; a.critPct = 0;
                    }),
                };
            });
        }

        // el Francotirador renegado: only fires from the rear (pos 3-4), hits anyone, big crits.
        public static UnitDef Sniper()
        {
            return UnitDef.New("Francotirador", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 9; u.armor = 0; u.dodge = 12; u.speed = 6; u.threat = 3;
                u.maxAmmo = 3;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Disparo de precisión", a => {
                        a.usableFrom = new[]{3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 90; a.dmgMin = 5; a.dmgMax = 9; a.critPct = 15; a.ammoCost = 1;
                    }),
                };
            });
        }

        // el Cabecilla: a leader whose death caves the cult's Moral (−15).
        public static UnitDef Ringleader()
        {
            return UnitDef.New("Cabecilla", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 16; u.armor = 1; u.dodge = 7; u.speed = 4; u.threat = 4;
                u.dropsMoraleOnDeath = 15;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Machete", a => {
                        a.usableFrom = new[]{1,2}; a.targetPos = new[]{1,2};
                        a.accuracy = 78; a.dmgMin = 4; a.dmgMax = 7; a.critPct = 6;
                    }),
                    AbilityDef.New("Revólver del jefe", a => {
                        a.usableFrom = new[]{2,3}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 80; a.dmgMin = 3; a.dmgMax = 6; a.critPct = 5;
                    }),
                };
            });
        }

        // el Rescatado: a freed prisoner (Rescate node). Joins as a 5th, non-controllable figure;
        // fragile, cowers, cannot fight. Keep him alive and extract him — his death fails the mission.
        public static UnitDef Vip()
        {
            return UnitDef.New("Rescatado", u =>
            {
                u.team = Team.Heroes;
                u.maxHP = 14; u.armor = 0; u.dodge = 6; u.speed = 2; u.corrResist = 0;
                u.isVip = true;
                u.countsForCohesion = false;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Encogerse", a => { a.targetKind = TargetKind.Self; a.accuracy = 0; }),
                };
            });
        }

        // ---------------- STAGE 1 BOSS & MINI-BOSSES (slice §4.3-4.4) ----------------

        /// <summary>
        /// El Predicador de la Mano — jefe de Stage 1. 90 PV y una Voluntad (Cohesión
        /// propia) de 60 que baja DOBLE con habilidades de Fe: dos vías de victoria.
        /// Fase 1 sermonea (+Corrupción de área); a &lt;50% PV se revela híbrido (fase 2:
        /// pierde el sermón, pega más fuerte).
        /// </summary>
        public static UnitDef Baron()
        {
            return UnitDef.New("Barón de Trinchera", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 90; u.armor = 1; u.dodge = 8; u.speed = 5;
                u.isBoss = true; u.ownCohesion = 60; u.phaseTwoAtPct = 50;
                u.countsForCohesion = true;
                u.resStun = 60; u.resMove = 70; u.resDebuff = 40;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Sermón de la Herida", a => {
                        a.targetKind = TargetKind.Enemy; a.accuracy = 0;
                        a.areaCorruption = 10; a.bossPhaseOnly = 1; // phase 1 only
                    }),
                    AbilityDef.New("Bendición del cuchillo", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 80; a.dmgMin = 5; a.dmgMax = 9; a.critPct = 8;
                    }),
                    AbilityDef.New("Garras del Tocado", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 85; a.dmgMin = 9; a.dmgMax = 15; a.critPct = 12;
                        a.bossPhaseOnly = 2; // revealed hybrid: only in phase 2
                        a.status = new StatusApply{ kind = StatusKind.Bleed, chance = 60, power = 3, duration = 2 };
                    }),
                };
            });
        }

        /// <summary>
        /// El Predicador Hueco — mini-boss errante. Mientras esté en pos. 3-4 sermonea
        /// (+Corrupción de área); si lo empujas a pos. 1-2 queda Silenciado y pega flojo.
        /// </summary>
        public static UnitDef FalseChaplain()
        {
            return UnitDef.New("Falso Capellán", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 45; u.armor = 1; u.dodge = 6; u.speed = 4;
                u.isElite = true; u.countsForCohesion = false;
                u.resStun = 30;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Sermón Falso", a => {
                        a.usableFrom = new[]{3,4}; a.targetKind = TargetKind.Enemy; a.accuracy = 0;
                        a.areaCorruption = 10;
                    }),
                    AbilityDef.New("Manotazo", a => {
                        a.usableFrom = new[]{1,2}; a.targetPos = new[]{1,2};
                        a.accuracy = 70; a.dmgMin = 2; a.dmgMax = 4; a.critPct = 0;
                    }),
                };
            });
        }

        /// <summary>
        /// La Madre de la Camada — mini-boss. Invoca 2 Cultistas por ronda hasta que
        /// destruyas los nidos (unidades Nido, 12 PV, arden ×2) que la escoltan.
        /// </summary>
        public static UnitDef Matriarch()
        {
            return UnitDef.New("Matriarca de la Herida", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 90; u.armor = 1; u.dodge = 4; u.speed = 3;
                u.isBoss = true; u.ownCohesion = 55; u.phaseTwoAtPct = 50; // JEFE del Sector 2 (las Galerías)
                u.isElite = true; u.countsForCohesion = false;
                u.threatChannel = ThreatChannel.Foso; // living wound: strikes into the dark
                u.summonType = "Cultist"; u.summonPerRound = 2; u.summonFromNest = true; // su camada solo acude desde los Nidos
                u.resStun = 50; u.resMove = 60;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Zarpa de la Matriarca", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 80; a.dmgMin = 6; a.dmgMax = 10; a.critPct = 5;
                    }),
                };
            });
        }

        public static UnitDef Nest()
        {
            return UnitDef.New("Nido de carne", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 12; u.armor = 0; u.dodge = 0; u.speed = 1;
                u.countsForCohesion = false; u.weakToFire = true; // arde ×2
                u.threatChannel = ThreatChannel.Foso; // a growth of the Foso
                u.resStun = 100; u.resMove = 100; // an object: can't be stunned or pushed
                u.abilities = new List<AbilityDef>
                {
                    // A nest does nothing on its turn; it just has to be destroyed
                    AbilityDef.New("Pulsar", a => {
                        a.targetKind = TargetKind.Self; a.accuracy = 0;
                    }),
                };
            });
        }

        // ---------------- CARNE DEL FOSO (§9.2, Sector 2+) ----------------
        // Injertos de carne del Infierno: sin Moral, débiles al fuego ×2, canal Foso (cazan en la sombra).

        /// <summary>Devoracadáveres: eats a corpse on the field each turn and heals — punishes long fights.</summary>
        public static UnitDef Gorger()
        {
            return UnitDef.New("Devoracadáveres", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 16; u.armor = 0; u.dodge = 6; u.speed = 5; u.threat = 3;
                u.countsForCohesion = false; u.weakToFire = true; u.threatChannel = ThreatChannel.Foso;
                u.devoursCorpses = true; u.corpseHeal = 8;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Dentellada", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 78; a.dmgMin = 4; a.dmgMax = 7; a.critPct = 5;
                    }),
                };
            });
        }

        /// <summary>Supurante: bursts a Gas cloud onto whoever wounds it, and a wider burst when it dies.</summary>
        public static UnitDef Weeper()
        {
            return UnitDef.New("Supurante", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 14; u.armor = 1; u.dodge = 4; u.speed = 3; u.threat = 3;
                u.countsForCohesion = false; u.weakToFire = true; u.threatChannel = ThreatChannel.Foso;
                u.gasOnHit = true; u.gasOnDeath = true;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Zarpa purulenta", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 75; a.dmgMin = 3; a.dmgMax = 6; a.critPct = 0;
                    }),
                };
            });
        }

        /// <summary>Cosido: two corpses stitched into one; at half HP it splits into two faster halves.</summary>
        public static UnitDef Stitched()
        {
            return UnitDef.New("Cosido", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 22; u.armor = 1; u.dodge = 3; u.speed = 3; u.threat = 4;
                u.countsForCohesion = false; u.weakToFire = true; u.threatChannel = ThreatChannel.Foso;
                u.splitsInto = "StitchedHalf";
                u.resStun = 30; u.resMove = 40;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Garfios cosidos", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 78; a.dmgMin = 5; a.dmgMax = 9; a.critPct = 5;
                    }),
                };
            });
        }

        /// <summary>The two halves a Cosido bursts into: small, quick, no further splitting.</summary>
        public static UnitDef StitchedHalf()
        {
            return UnitDef.New("Media res", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 10; u.armor = 0; u.dodge = 8; u.speed = 6; u.threat = 1;
                u.countsForCohesion = false; u.weakToFire = true; u.threatChannel = ThreatChannel.Foso;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Zarpazo cosido", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 80; a.dmgMin = 3; a.dmgMax = 5; a.critPct = 5;
                    }),
                };
            });
        }

        /// <summary>Reptante: a legless torso that drags itself fast; very hard to hit.</summary>
        public static UnitDef Crawler()
        {
            return UnitDef.New("Reptante", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 10; u.armor = 0; u.dodge = 16; u.speed = 8; u.threat = 2;
                u.countsForCohesion = false; u.weakToFire = true; u.threatChannel = ThreatChannel.Foso;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Mordisco reptante", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 82; a.dmgMin = 3; a.dmgMax = 6; a.critPct = 8;
                        a.status = new StatusApply{ kind = StatusKind.Bleed, chance = 40, power = 2, duration = 2 };
                    }),
                };
            });
        }

        /// <summary>Boca en la Pared: a fixed maw that swallows a hero (heavy damage + Stun).</summary>
        public static UnitDef Maw()
        {
            return UnitDef.New("Boca en la Pared", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 26; u.armor = 3; u.dodge = 0; u.speed = 2; u.threat = 4;
                u.countsForCohesion = false; u.weakToFire = true; u.threatChannel = ThreatChannel.Foso;
                u.resStun = 100; u.resMove = 100; // fixed: can't be stunned or pushed
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Tragar", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 80; a.dmgMin = 6; a.dmgMax = 10; a.critPct = 5;
                        a.status = new StatusApply{ kind = StatusKind.Stun, chance = 60, power = 0, duration = 1 };
                    }),
                };
            });
        }

        /// <summary>Verdugo (élite): fast and brutal; acts twice if nothing hurt it, and stains on hit.</summary>
        public static UnitDef Butcher()
        {
            return UnitDef.New("Verdugo", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 26; u.armor = 0; u.dodge = 18; u.speed = 9; u.threat = 5;
                u.countsForCohesion = false; u.weakToFire = true; u.threatChannel = ThreatChannel.Foso;
                u.isElite = true; u.extraActionIfUnhit = true;
                u.resStun = 40; u.resMove = 40;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Tajo del Verdugo", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 82; a.dmgMin = 8; a.dmgMax = 13; a.critPct = 15;
                        a.corruptionDelta = 10; // +10 Mancha a quien golpea
                    }),
                };
            });
        }

        // ---------------- DESCENDIDOS (§9.3, Sector 3 y Reducto) ----------------
        // Demonios del Foso: sin Moral, canal Foso, isDescendido (+5 Mancha al grupo al aparecer).

        /// <summary>Sabueso del Foso: jauría veloz; marca a un héroe y el resto le cae encima (foco).</summary>
        public static UnitDef Houndof()
        {
            return UnitDef.New("Sabueso del Foso", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 15; u.armor = 0; u.dodge = 14; u.speed = 9; u.threat = 3;
                u.countsForCohesion = false; u.threatChannel = ThreatChannel.Foso; u.isDescendido = true;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Dentellada de jauría", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 80; a.dmgMin = 4; a.dmgMax = 7; a.critPct = 8;
                        a.status = new StatusApply{ kind = StatusKind.Marked, chance = 100, power = 15, duration = 2 }; // marca: la jauría se ceba
                    }),
                };
            });
        }

        /// <summary>Corista: canta nombres del Foso — silencia los Milagros del grupo y +Mancha en área.</summary>
        public static UnitDef Chorister()
        {
            return UnitDef.New("Corista", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 12; u.armor = 0; u.dodge = 9; u.speed = 5; u.threat = 3;
                u.countsForCohesion = false; u.threatChannel = ThreatChannel.Foso; u.isDescendido = true;
                u.silencesMiracles = true;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Canto del Foso", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 70; a.dmgMin = 2; a.dmgMax = 4; a.critPct = 0; a.corruptionDelta = 4;
                    }),
                };
            });
        }

        /// <summary>Marcador: quema una marca en un héroe (+30% daño recibido hasta que el Marcador muera).</summary>
        public static UnitDef Brander()
        {
            return UnitDef.New("Marcador", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 18; u.armor = 2; u.dodge = 6; u.speed = 6; u.threat = 4;
                u.countsForCohesion = false; u.threatChannel = ThreatChannel.Foso; u.isDescendido = true;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Marca del Foso", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 100; a.dmgMin = 1; a.dmgMax = 3; a.critPct = 0;
                        a.status = new StatusApply{ kind = StatusKind.Marked, chance = 100, power = 30, duration = 99 }; // hasta que el Marcador caiga
                    }),
                };
            });
        }

        /// <summary>Guardián de la Herida (JEFE S3, élite): ancla demoníaca; mientras vive, los Descendidos reviven una vez.</summary>
        public static UnitDef Warden()
        {
            return UnitDef.New("Guardián de la Herida", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 80; u.armor = 3; u.dodge = 5; u.speed = 5; u.threat = 8;
                u.countsForCohesion = false; u.threatChannel = ThreatChannel.Foso; u.isDescendido = true;
                u.isElite = true; u.revivesDescendidos = true;
                u.isBoss = true; u.ownCohesion = 45; u.phaseTwoAtPct = 50; // JEFE del Sector 3 (la Tierra de Nadie)
                u.resStun = 60; u.resMove = 70;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Zarpa del Guardián", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 80; a.dmgMin = 6; a.dmgMax = 11; a.critPct = 6; a.corruptionDelta = 5;
                    }),
                };
            });
        }

        // ---------------- JEFE FINAL (§9.5, el Reducto) ----------------
        /// <summary>Confesor Rojo (jefe final, Hito 7): 3 fases — humano (predica) → carne abierta
        /// (body horror, +daño) → poseído por un Verdugo (invoca uno y entra en frenesí).</summary>
        public static UnitDef Confessor()
        {
            return UnitDef.New("Confesor Rojo", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 140; u.armor = 2; u.dodge = 5; u.speed = 4; u.threat = 12;
                u.isBoss = true; u.ownCohesion = 70; u.countsForCohesion = false;
                u.phaseTwoAtPct = 66; u.phaseThreeAtPct = 33;
                u.threatChannel = ThreatChannel.Human; // el profeta del culto: un hombre hasta el final
                u.summonType = "Cultist"; u.summonPerRound = 1;   // fase 1: predica y su grey acude
                u.phaseThreeSummon = "Butcher";                    // fase 3: un Verdugo lo posee
                u.phaseTwoLog = "‼ La carne del Confesor se ABRE: la Herida habla por su boca. Deja el sermón y desgarra.";
                u.phaseThreeLog = "‼ Un VERDUGO se derrama dentro del Confesor: la posesión es total. Frenesí.";
                u.resStun = 60; u.resMove = 70;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Sermón del Fin", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 80; a.dmgMin = 5; a.dmgMax = 9; a.critPct = 4; a.corruptionDelta = 8;
                    }),
                    AbilityDef.New("Zarpa de la Herida", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 82; a.dmgMin = 8; a.dmgMax = 14; a.critPct = 8;
                    }),
                };
            });
        }

        // ---------------- MINI-BOSSES profundos (§9.5) ----------------
        /// <summary>Alfa del Foso (mini-boss S3): líder de una jauría de Sabuesos. Marca a su presa y
        /// la jauría se ceba; matarlo la dispersa (packLeader).</summary>
        public static UnitDef Alpha()
        {
            return UnitDef.New("Alfa del Foso", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 38; u.armor = 1; u.dodge = 12; u.speed = 8; u.threat = 6;
                u.countsForCohesion = false; u.threatChannel = ThreatChannel.Foso; u.isDescendido = true;
                u.isElite = true; u.packLeader = true;
                u.resStun = 40; u.resMove = 50;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Aullido de caza", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 85; a.dmgMin = 5; a.dmgMax = 9; a.critPct = 8;
                        a.status = new StatusApply{ kind = StatusKind.Marked, chance = 100, power = 25, duration = 3 }; // la jauría se ceba en el Marcado
                    }),
                };
            });
        }

        /// <summary>Desollador (mini-boss F, Lictor de biomasa): arranca a un héroe de la formación
        /// y lo desuella cada ronda hasta que lo abaten (isFlayer, fuerza el aislamiento).</summary>
        public static UnitDef Flayer()
        {
            return UnitDef.New("Desollador", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 46; u.armor = 2; u.dodge = 9; u.speed = 7; u.threat = 7;
                u.countsForCohesion = false; u.threatChannel = ThreatChannel.Foso; u.isDescendido = true;
                u.isElite = true; u.isFlayer = true;
                u.resStun = 30; u.resMove = 80;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Cuchillas de hueso", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 82; a.dmgMin = 6; a.dmgMax = 10; a.critPct = 6;
                    }),
                };
            });
        }

        // ---------------- FAUNA (§9.4, la Tierra de Nadie, neutral) ----------------
        // Bestias mutadas bajo el cielo rojo: no son la Congregación. Canal Human (cazan lo que ven,
        // a lo iluminado/expuesto), isFauna (respiro tonal, sueltan carroña). Sin isDescendido → sin +5.

        /// <summary>Plaga de ratas: enjambre de mordiscos débiles con Sangrado; el daño de área la destroza.</summary>
        public static UnitDef RatSwarm()
        {
            return UnitDef.New("Plaga de ratas", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 12; u.armor = 0; u.dodge = 10; u.speed = 7; u.threat = 2;
                u.countsForCohesion = false; u.threatChannel = ThreatChannel.Human;
                u.isFauna = true; u.weakToArea = true;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Mordiscos del enjambre", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 75; a.dmgMin = 2; a.dmgMax = 4; a.critPct = 4;
                        a.status = new StatusApply{ kind = StatusKind.Bleed, chance = 60, power = 2, duration = 2 };
                    }),
                };
            });
        }

        /// <summary>Cuervo carroñero: vuela (ignora posición); picotea ojos y baja la precisión del golpeado.</summary>
        public static UnitDef Carrion()
        {
            return UnitDef.New("Cuervo carroñero", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 9; u.armor = 0; u.dodge = 15; u.speed = 8; u.threat = 2;
                u.countsForCohesion = false; u.threatChannel = ThreatChannel.Human; u.isFauna = true;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Picotazo a los ojos", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 80; a.dmgMin = 2; a.dmgMax = 4; a.critPct = 4;
                        a.status = new StatusApply{ kind = StatusKind.AccDebuff, chance = 80, power = 12, duration = 2 };
                    }),
                };
            });
        }

        /// <summary>Bestia del Alambre: enredada en alambre de espino; al golpearla al melé, el atacante sangra.</summary>
        public static UnitDef WireBeast()
        {
            return UnitDef.New("Bestia del Alambre", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 24; u.armor = 1; u.dodge = 4; u.speed = 3; u.threat = 3;
                u.countsForCohesion = false; u.threatChannel = ThreatChannel.Human;
                u.isFauna = true; u.bleedsAttackerOnMelee = true;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Latigazo de espino", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 70; a.dmgMin = 4; a.dmgMax = 8; a.critPct = 6;
                        a.status = new StatusApply{ kind = StatusKind.Bleed, chance = 50, power = 3, duration = 2 };
                    }),
                };
            });
        }

        /// <summary>Carroñero mutado: roba un suministro del inventario si llega y luego huye.</summary>
        public static UnitDef Scavver()
        {
            return UnitDef.New("Carroñero mutado", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 17; u.armor = 0; u.dodge = 8; u.speed = 6; u.threat = 2;
                u.countsForCohesion = false; u.threatChannel = ThreatChannel.Human;
                u.isFauna = true; u.stealsSupply = true;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Zarpazo hambriento", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 75; a.dmgMin = 3; a.dmgMax = 6; a.critPct = 5;
                    }),
                };
            });
        }

        // ---------------- MINI-BOSS del Sector 2 ----------------

        /// <summary>Zapador Injertado (mini-boss S2): the Congregation's engineer, grafted with Foso flesh.
        /// Telegraphs a gallery cave-in that buries whoever pushed forward into the dark.</summary>
        public static UnitDef Sapper()
        {
            return UnitDef.New("Zapador Injertado", u =>
            {
                u.team = Team.Enemies;
                u.maxHP = 44; u.armor = 2; u.dodge = 4; u.speed = 3; u.threat = 6;
                u.countsForCohesion = false; u.weakToFire = true; u.threatChannel = ThreatChannel.Foso;
                u.isElite = true; u.isSapper = true;
                u.resStun = 50; u.resMove = 60;
                u.abilities = new List<AbilityDef>
                {
                    AbilityDef.New("Pico injertado", a => {
                        a.usableFrom = new[]{1,2,3,4}; a.targetPos = new[]{1,2,3,4};
                        a.accuracy = 80; a.dmgMin = 6; a.dmgMax = 10; a.critPct = 6;
                    }),
                };
            });
        }
    }
}
