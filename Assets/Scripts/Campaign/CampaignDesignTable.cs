namespace Emberline.Campaign
{
    /// <summary>
    /// The design layer for all hundred missions: role, the approaches offered
    /// at the door, who may come, which camp or village the mission belongs to,
    /// what it remembers and what it makes the player want next.
    ///
    /// Read this beside CampaignTable.cs. That file is the story; this one is
    /// the game. A mission whose role is Assault and whose approach list has one
    /// entry is a mission that failed the redesign and should be reopened.
    /// </summary>
    public static class CampaignDesign
    {
        private static Approach[] A(params Approach[] a) => a;
        private static Companion[] C(params Companion[] c) => c;
        private static string[] S(params string[] s) => s;

        private const Approach Front = Approach.Assault, Quiet = Approach.Stealth,
            Ambush = Approach.Ambush, Burn = Approach.Sabotage, Allied = Approach.Allied;

        private const Companion Suzu = Companion.Suzu, Fumi = Companion.Fumi,
            Tsuru = Companion.Tsuru, Daigo = Companion.Daigo, Toku = Companion.Toku,
            Nire = Companion.Nire;

        private static MissionDesign D(int id, MissionRole role, string want,
            Approach[] approaches = null, string approachFlag = "", Companion[] companions = null,
            CampId camp = CampId.None, VillageId village = VillageId.None,
            string[] sets = null, string[] reads = null, string consequence = "") => new()
        {
            id = id, role = role, want = want,
            approaches = approaches ?? System.Array.Empty<Approach>(),
            approachFlag = approachFlag,
            companions = companions ?? System.Array.Empty<Companion>(),
            camp = camp, village = village,
            sets = sets ?? System.Array.Empty<string>(),
            reads = reads ?? System.Array.Empty<string>(),
            consequence = consequence,
        };

        // ---------------------------------------------------------------
        // The six companions.
        // ---------------------------------------------------------------
        public static readonly CompanionDef[] Companions =
        {
            new() { id = Companion.Suzu, name = "SUZU", role = "SCOUT", joinMission = 12, fights = true,
                joins = "Stealing from the same convoy Renzo is shadowing, badly.",
                specialty = "Patrol routes, second entrances, alarms. She is why a stealth approach exists before mission 40.",
                weakness = "She runs when a fight turns, and the mission does not fail, which is worse.",
                line = "You're going to get her back. People do." },
            new() { id = Companion.Fumi, name = "FUMI", role = "INFORMANT", joinMission = 14, fights = false,
                joins = "The survivor of the burned village, who was in the cellar copying a ledger.",
                specialty = "Reads the enemy's paper. One intact document with Fumi replaces a whole night of scouting.",
                weakness = "She will not destroy records. Ever. Including the ones killing people.",
                line = "I am not on your side. I am on the side of knowing." },
            new() { id = Companion.Tsuru, name = "TSURU", role = "ARCHER", joinMission = 26, fights = true,
                joins = "A conscript archer who walked off a wall two years ago and buried the officer who refused.",
                specialty = "Height. Given a roof or a ridge he covers an approach, which is what makes an ambush survivable.",
                weakness = "Arrows are finite and counted. A village left to burn does not resupply him.",
                line = "I shot at people like you for two years. I was not good at it." },
            new() { id = Companion.Daigo, name = "DAIGO", role = "WARRIOR", joinMission = 33, fights = true,
                joins = "Taken out of Goro's pens with a smith's collar still on him.",
                specialty = "He holds. A gate, a bridge, a door, a line of villagers.",
                weakness = "He cannot be quiet. No mission carrying Daigo offers a stealth approach.",
                line = "I will be at the gate. That is all I am for. Don't waste it." },
            new() { id = Companion.Toku, name = "TOKU", role = "BLACKSMITH", joinMission = 36, fights = false,
                joins = "The smith who marked every blade he was forced to make, so that one day somebody would ask why.",
                specialty = "Weapons. Steel brought back from a raid becomes an upgrade rather than a shop purchase.",
                weakness = "He needs materials, and materials come from villages and camps.",
                line = "You took eleven years to come and ask me." },
            new() { id = Companion.Nire, name = "NIRE", role = "HEALER", joinMission = 46, fights = false,
                joins = "The marsh herbalist who knew Renzo's father when he came through carrying something wrapped in cloth.",
                specialty = "Medicine that persists between missions, and the only safe route through water.",
                weakness = "Slow, and she will not enter a fight. A mission with Nire has a person in it who can die.",
                line = "I knew two men who were sure. Be a third thing." },
        };

        // ---------------------------------------------------------------
        // The eight camps. A camp is a place, not a level.
        // ---------------------------------------------------------------
        public static readonly CampDef[] Camps =
        {
            new() { id = CampId.TollPost, name = "THE TOLL POST", garrison = 26, missions = new[] { 3, 4, 5 },
                marks = new[] { "the bell on the gate tower", "the prisoner pen behind the barricade",
                    "Goro's own post, lit all night", "the ledger table", "the pass rotation at the third hour",
                    "the cart track nobody watches" } },
            new() { id = CampId.BrokenBanner, name = "THE BROKEN BANNER", garrison = 31, missions = new[] { 11, 13, 15, 16, 17 },
                marks = new[] { "the watchtower that sees the whole valley", "the supply warehouse",
                    "the elder's house with the floor pulled up", "the scavengers' blacksmith",
                    "one prisoner nobody has bothered to move", "the dry culvert under the east wall",
                    "the patrol that walks the ridge and not the road" } },
            new() { id = CampId.SilentCamp, name = "THE SILENT CAMP", garrison = 28, missions = new[] { 22, 24, 25, 28, 29 },
                marks = new[] { "three signal fires laid but unlit", "the commander's silk cord",
                    "the pen with a girl in it who is not Aiko", "the archers on the bamboo scaffold",
                    "the path the Three Blades use and no one else", "the way out that closes behind you" } },
            new() { id = CampId.ThePens, name = "THE PENS", garrison = 40, missions = new[] { 31, 32, 33, 34 },
                marks = new[] { "the execution platform", "four pens, and only one is guarded properly",
                    "the wagon yard and its drivers", "the alarm horn on the north tower",
                    "Goro's officers eat apart from the men", "a collared prisoner the size of a door",
                    "the quarry road, unwatched, downhill" } },
            new() { id = CampId.SunkenCamp, name = "THE SUNKEN CAMP", garrison = 24, missions = new[] { 42, 43, 44 },
                marks = new[] { "the records chest above the waterline", "what is standing in the deep water",
                    "the causeway that floods at the turn", "a patrol that never came back",
                    "reed smoke from somewhere that should be empty" } },
            new() { id = CampId.Garrison, name = "THE GARRISON", garrison = 33, missions = new[] { 62, 64, 65, 67 },
                marks = new[] { "Jin's men drill in the rain and nobody makes them",
                    "the portrait in the barracks hall", "the house at the end of the street, already burned",
                    "the dueling ring the town uses on rest days", "the road out that Jin leaves open" } },
            new() { id = CampId.FrozenCamp, name = "THE FROZEN CAMP", garrison = 30, missions = new[] { 71, 72, 73 },
                marks = new[] { "the magazine, and how much of the slope is above it",
                    "guns pointed down a road an army would use", "men dying of the cold, not of you",
                    "the supply line that stops coming", "the cornice that has not fallen yet" } },
            new() { id = CampId.IronFortress, name = "THE IRON FORTRESS", garrison = 40, missions = new[] { 74, 75, 76, 77, 78, 79, 83 },
                marks = new[] { "the drain under the outer wall", "the armoury full of Yorune steel",
                    "the prison tower, lit at the top", "the Iron Guard's captain, and what breaks them",
                    "the inner gate and the man who will not leave it",
                    "half the garrison is not Kagehira's and knows it" } },
            new() { id = CampId.SummitRoad, name = "THE SUMMIT ROAD", garrison = 25, missions = new[] { 93, 94, 95, 96 },
                marks = new[] { "the last guard post, already abandoned", "his own guard, dead on the road",
                    "the fog bank that does not move with the wind", "the stair, and what is waiting inside the door" } },
        };

        // ---------------------------------------------------------------
        // The five villages. Trust buys supply, not statistics.
        // ---------------------------------------------------------------
        public static readonly VillageDef[] Villages =
        {
            new() { id = VillageId.Yorune, name = "YORUNE", firstMission = 1,
                gives = "Nothing. It is ash until mission 100, and that is the point." },
            new() { id = VillageId.Ashfall, name = "ASHFALL", firstMission = 13,
                gives = "Hunters, and the arrows Tsuru counts." },
            new() { id = VillageId.Kiba, name = "KIBA", firstMission = 36,
                gives = "Steel, and Toku's forge. At full trust, militia for an allied assault." },
            new() { id = VillageId.ReedVillage, name = "THE REED VILLAGE", firstMission = 46,
                gives = "Medicine, marsh routes, and Nire." },
            new() { id = VillageId.GarrisonTown, name = "THE GARRISON TOWN", firstMission = 62,
                gives = "Information, and the only place in the game where nobody is hunting anybody." },
        };

        // ---------------------------------------------------------------
        // The hundred, as designed rather than as narrated.
        // ---------------------------------------------------------------
        public static readonly MissionDesign[] Designs =
        {
            // ===== CHAPTER 1 — ASHES OF YORUNE. Renzo is alone, and the game
            // spends ten missions making sure the player knows what that costs.
            D(1, MissionRole.Discovery, "I want to know who is still here.",
                village: VillageId.Yorune, sets: S("yorune_seen"),
                consequence: "Yorune is on the map now, and it is the only place on it."),
            D(2, MissionRole.Assault, "I want to know what a Black Seal is.",
                A(Front, Ambush, Quiet), companions: null, camp: CampId.None,
                sets: S("convoy_broken"),
                consequence: "The Lantern Road carries merchants again within the chapter. The first supply run is possible."),
            D(3, MissionRole.Recon, "I want to get inside that post before the pass closes.",
                A(Quiet), camp: CampId.TollPost, sets: S("toll_watched"),
                consequence: "The toll post's card is filled in as far as Renzo got: the bell, the pen, the rotation."),
            D(4, MissionRole.Assault, "I want the man whose mark is on every order.",
                A(Front, Quiet, Burn), approachFlag: "toll_watched", camp: CampId.TollPost,
                reads: S("toll_watched"), sets: S("toll_open")),
            D(5, MissionRole.Story, "I want to know if my sister is alive.",
                camp: CampId.TollPost, reads: S("toll_open"), sets: S("aiko_named"),
                consequence: "Goro is beaten and not dead. The valley knows a Kurogawa is walking it."),
            D(6, MissionRole.Discovery, "I want to follow her.", reads: S("aiko_named")),
            D(7, MissionRole.Story, "I want to know who cut my father's mark into that tree."),
            D(8, MissionRole.Discovery, "I want to know what my father was doing out here.",
                sets: S("father_route")),
            D(9, MissionRole.Story, "I want to see what all those lanterns are for.", reads: S("father_route")),
            D(10, MissionRole.Consequence, "I want to know why the Seal has my family's name on it.",
                sets: S("seal_fragment"),
                consequence: "The fragment is the first physical proof. Fumi will be able to read it, when there is a Fumi."),

            // ===== CHAPTER 2 — THE LANTERN NETWORK. The first two companions,
            // the first camp that is a place, and the first time preparation pays.
            D(11, MissionRole.Prepare, "I want to see how big this actually is.",
                A(Burn, Quiet), camp: CampId.BrokenBanner, sets: S("supply_burned"),
                consequence: "Eight fewer defenders at the Broken Banner, and its blacksmith has no coal."),
            D(12, MissionRole.Story, "I want her to stop following me and start telling me things.",
                A(Quiet, Ambush), companions: C(Suzu), sets: S("suzu_joined"),
                consequence: "SUZU joins. Patrol rotations become readable; the first stealth approaches open."),
            D(13, MissionRole.Assault, "I want to know who else burned, and why.",
                A(Front, Quiet, Ambush, Burn), approachFlag: "supply_burned",
                companions: C(Suzu), camp: CampId.BrokenBanner, village: VillageId.Ashfall,
                reads: S("supply_burned"), sets: S("ashfall_freed"),
                consequence: "The Scavenger King falls. Ashfall's stores come back and its hunters come out of the hills."),
            D(14, MissionRole.Story, "I want somebody who remembers her face.",
                A(Front, Allied), companions: C(Suzu), village: VillageId.Ashfall,
                reads: S("ashfall_freed"), sets: S("fumi_joined"),
                consequence: "FUMI joins. Captured documents now convert into camp intel without a scouting run."),
            D(15, MissionRole.Recon, "I want the tower that sees the whole valley.",
                A(Quiet), companions: C(Suzu), camp: CampId.BrokenBanner, sets: S("banner_watched")),
            D(16, MissionRole.Assault, "I want them to come to me for once.",
                A(Front, Ambush, Allied), approachFlag: "banner_watched",
                companions: C(Suzu, Fumi), camp: CampId.BrokenBanner, village: VillageId.Ashfall,
                reads: S("banner_watched", "ashfall_freed"), sets: S("valley_mapped"),
                consequence: "The valley is mapped: three territories, and a fourth marked only with a serpent."),
            D(17, MissionRole.Personal, "I want what he is carrying.",
                A(Front, Ambush), companions: C(Suzu), camp: CampId.BrokenBanner),
            D(18, MissionRole.Story, "I want to read it.",
                A(Quiet, Front), companions: C(Fumi), reads: S("valley_mapped"), sets: S("cipher_key")),
            D(19, MissionRole.Personal, "I want six years of prisoner rolls.",
                A(Quiet, Front, Ambush), companions: C(Fumi, Suzu), reads: S("cipher_key"),
                sets: S("rolls_taken"),
                consequence: "FUMI's own handwriting is in the rolls. She does not mention it for forty missions."),
            D(20, MissionRole.Consequence, "I want them to know I am coming.",
                A(Burn, Quiet, Allied), companions: C(Suzu, Fumi), village: VillageId.Ashfall,
                reads: S("rolls_taken"), sets: S("named_by_enemy"),
                consequence: "The last signal reads KUROGAWA IS COMING. From here the world hunts back."),

            // ===== CHAPTER 3 — THE SILENT FOREST. The chapter where the player
            // is prey, and the first mission the player is meant to walk out of.
            D(21, MissionRole.Story, "I want to know what that was.", reads: S("named_by_enemy")),
            D(22, MissionRole.Recon, "I want to know who is paying them.",
                A(Quiet), companions: C(Suzu), camp: CampId.SilentCamp, sets: S("forest_watched")),
            D(23, MissionRole.Discovery, "I want to know who kills their own officers.",
                companions: C(Suzu)),
            D(24, MissionRole.Story, "I want to know who sent them for me by name.",
                A(Front, Ambush), companions: C(Suzu), camp: CampId.SilentCamp,
                sets: S("blades_beaten"),
                consequence: "Assassination ambushes stop appearing on travel beats for the rest of Act I."),
            D(25, MissionRole.Assault, "I want the man the silk cord belongs to.",
                A(Burn, Quiet, Front), approachFlag: "forest_watched",
                companions: C(Suzu), camp: CampId.SilentCamp, reads: S("forest_watched", "blades_beaten")),
            D(26, MissionRole.Story, "I want the archer on my side, not above me.",
                A(Front, Ambush), companions: C(Suzu, Tsuru), sets: S("tsuru_joined"),
                consequence: "TSURU joins. Any mission with height now has covering fire, and ambush becomes survivable."),
            D(27, MissionRole.Downtime, "I want to follow the thread.",
                companions: C(Suzu, Tsuru), sets: S("red_thread"),
                consequence: "No enemies, and the game never says so. The red thread is the only warm colour in the campaign and this is the mission that is only that."),
            D(28, MissionRole.Assault, "I want the girl out even though she is not my sister.",
                A(Quiet, Front, Allied), approachFlag: "forest_watched",
                companions: C(Suzu, Tsuru), camp: CampId.SilentCamp, reads: S("forest_watched"),
                consequence: "A rescued stranger walks to Ashfall and says who sent her. Ashfall's trust rises."),
            D(29, MissionRole.Story, "I want out of a room that is closing.",
                camp: CampId.SilentCamp, reads: S("red_thread")),
            D(30, MissionRole.Consequence, "I want the toll-captain's country.",
                companions: C(Tsuru), sets: S("marsh_open"),
                consequence: "The Pale Shade dies. The marsh becomes crossable at night, and Act II has a direction."),

            // ===== CHAPTER 4 — GORO'S TERRITORY. The largest camp in Act II,
            // the first village-led battle, and the first companion the player
            // can get killed.
            D(31, MissionRole.Recon, "I want to know how many of them there are.",
                A(Quiet), companions: C(Suzu, Tsuru), camp: CampId.ThePens,
                reads: S("marsh_open"), sets: S("pens_watched")),
            D(32, MissionRole.Assault, "I want the wagons stopped, not followed.",
                A(Ambush, Front, Quiet), approachFlag: "pens_watched",
                companions: C(Suzu, Tsuru), camp: CampId.ThePens, reads: S("pens_watched"),
                sets: S("wagons_freed"),
                consequence: "Freed villagers walk to Kiba instead of into the hills. Kiba starts to exist."),
            D(33, MissionRole.Assault, "I want that man out of the collar.",
                A(Front, Quiet, Burn, Allied), approachFlag: "pens_watched",
                companions: C(Suzu, Tsuru), camp: CampId.ThePens,
                reads: S("pens_watched", "wagons_freed"), sets: S("daigo_joined", "pens_burned", "kiba_arms"),
                consequence: "DAIGO joins, and the pens stop being a place. Allied approaches become possible."),
            D(34, MissionRole.Defend, "I want to be at the platform before dawn.",
                A(Front, Allied, Ambush), companions: C(Daigo, Tsuru), camp: CampId.ThePens,
                reads: S("pens_burned"), sets: S("platform_held"),
                consequence: "Everyone on the platform lives. None of them is her, and the game does not soften that."),
            D(35, MissionRole.Personal, "I want to know why he counts his arrows.",
                A(Front, Ambush), companions: C(Tsuru, Daigo), reads: S("ashfall_freed"),
                sets: S("tsuru_told", "kiba_hunters"),
                consequence: "TSURU's own wall is named. He will not shoot at it later without saying so."),
            D(36, MissionRole.Story, "I want a forge that is mine.",
                A(Front, Allied), companions: C(Daigo, Suzu), village: VillageId.Kiba,
                sets: S("toku_joined", "kiba_forge", "kiba_barricades"),
                consequence: "TOKU joins and Kiba has a forge. Steel taken from camps becomes weapon tiers."),
            D(37, MissionRole.Defend, "I want this one village to still be here tomorrow.",
                A(Allied, Front), companions: C(Daigo, Tsuru, Toku), village: VillageId.Kiba,
                reads: S("kiba_barricades", "kiba_arms", "kiba_hunters", "kiba_forge"),
                sets: S("kiba_held"),
                consequence: "Kiba stands or does not, on what the player built. Its trust is the ceiling for every allied assault after."),
            D(38, MissionRole.Story, "I want to fight him somewhere I chose.",
                companions: C(Daigo), reads: S("kiba_held")),
            D(39, MissionRole.Assault, "I want the gate open and the wall empty.",
                A(Burn, Front, Allied), approachFlag: "pens_watched",
                companions: C(Daigo, Tsuru, Suzu), reads: S("kiba_held", "pens_burned"),
                sets: S("gate_burned")),
            D(40, MissionRole.Consequence, "I want the marsh.",
                companions: C(Daigo), reads: S("gate_burned"), sets: S("goro_dead"),
                consequence: "Goro dies. The pens empty, the valley repopulates, and Kiba can be rebuilt between missions."),

            // ===== CHAPTER 5 — INTO THE MARSH. Isolation on purpose: the
            // companions can come, and the marsh is a reason not to bring them.
            D(41, MissionRole.Story, "I want to know who is collecting lanterns.",
                companions: C(Daigo), reads: S("goro_dead")),
            D(42, MissionRole.Recon, "I want to find what is answering.",
                companions: C(Suzu), camp: CampId.SunkenCamp),
            D(43, MissionRole.Personal, "I want to know what he owes.",
                A(Front, Ambush), companions: C(Daigo), camp: CampId.SunkenCamp,
                sets: S("daigo_told", "sunken_read"),
                consequence: "DAIGO says the debt out loud, once. He never says it again, including at the gate."),
            D(44, MissionRole.Story, "I want to know whose crest that is.",
                companions: C(Tsuru), camp: CampId.SunkenCamp, reads: S("sunken_read"),
                sets: S("kurogane_crest")),
            D(45, MissionRole.Prepare, "I want to know who lives out here.",
                A(Quiet, Front), companions: C(Suzu), sets: S("reed_smoke"),
                consequence: "SUZU takes the long way round and finds the reed village first. She is proud of it."),
            D(46, MissionRole.Defend, "I want them to let me in.",
                A(Allied, Front), companions: C(Daigo, Tsuru), village: VillageId.ReedVillage,
                reads: S("reed_smoke"), sets: S("reed_trust"),
                consequence: "The reed village opens. Medicine, marsh routes, and a guide who knew Renzo's father."),
            D(47, MissionRole.Personal, "I want her to take me down there.",
                A(Front, Quiet), companions: C(Nire), village: VillageId.ReedVillage,
                reads: S("reed_trust"), sets: S("nire_joined", "marsh_route"),
                consequence: "NIRE joins. Water stops being a wall; one recovery per mission stops being a shop item."),
            D(48, MissionRole.Story, "I want the door with my father's mark on it.",
                reads: S("marsh_route")),
            D(49, MissionRole.Discovery, "I want to know what a lock with three keys is for.",
                sets: S("seal_understood")),
            D(50, MissionRole.Consequence, "I want to know what my father did.",
                A(Front, Quiet), reads: S("seal_understood"), sets: S("first_key"),
                consequence: "The first key. From here the campaign is about what his father chose, not who burned the village."),

            // ===== CHAPTER 6 — THE DROWNED TEMPLE. The memories are untouched:
            // they were already the best missions in the game. Around them,
            // the chapter gets the preparation and the walk-away it lacked.
            D(51, MissionRole.Discovery, "I want the rest of the journal.",
                companions: C(Nire), reads: S("first_key")),
            D(52, MissionRole.Memory, "I want to see the village alive."),
            D(53, MissionRole.Memory, "I want to reach my father's door."),
            D(54, MissionRole.Memory, "I want to hold the door."),
            D(55, MissionRole.Memory, "I want to know who carried it out."),
            D(56, MissionRole.Memory, "I want to know what she did with it.", sets: S("aiko_hid_it")),
            D(57, MissionRole.Discovery, "I want to read her handwriting.",
                A(Quiet, Front), companions: C(Nire), reads: S("aiko_hid_it"), sets: S("cell_read")),
            D(58, MissionRole.Assault, "I want the second key.",
                A(Quiet, Front, Ambush), approachFlag: "cell_read",
                companions: C(Suzu, Nire), reads: S("cell_read"), sets: S("second_key"),
                consequence: "SUZU comes back to the pen she could not open at 29 and opens it. The game does not remark on it."),
            D(59, MissionRole.Story, "I want what is under the guardian.",
                companions: C(Nire), reads: S("second_key"), sets: S("guardian_dead"),
                consequence: "The temple stops being hostile. The marsh route is permanent and Nire will use it alone."),
            D(60, MissionRole.Downtime, "I want the man who drew the map.",
                companions: C(Nire, Toku, Suzu), reads: S("guardian_dead"), sets: S("kurogane_named"),
                consequence: "No enemies. Renzo stops looking for answers, and the people around him notice the change first."),

            // ===== CHAPTER 7 — KUROGANE. The chapter where the companions stop
            // agreeing with Renzo, and one mission refuses to start.
            D(61, MissionRole.Story, "I want to know how he knows my sister's name.",
                reads: S("kurogane_named")),
            D(62, MissionRole.Discovery, "I want to know where he is leading me.",
                companions: C(Suzu), camp: CampId.Garrison, village: VillageId.GarrisonTown,
                sets: S("garrison_found")),
            D(63, MissionRole.Story, "I want to be good enough to make him try.",
                reads: S("garrison_found")),
            D(64, MissionRole.Personal, "I want to know what he was to my father.",
                A(Quiet, Front), companions: C(Fumi), camp: CampId.Garrison,
                village: VillageId.GarrisonTown, sets: S("portrait_found", "garrison_watched"),
                consequence: "FUMI finds her own hand in the garrison ledger. She tells Renzo, and he is not kind about it."),
            D(65, MissionRole.Assault, "I want to know why they say he tried to stop it.",
                A(Front, Ambush, Allied), approachFlag: "garrison_watched",
                companions: C(Daigo, Tsuru), camp: CampId.Garrison,
                reads: S("garrison_watched", "portrait_found")),
            D(66, MissionRole.Story, "I want him to agree to meet me.",
                companions: C(Toku), reads: S("kiba_forge"), sets: S("duel_won"),
                consequence: "Won with Toku's steel or without it, and the fight is visibly different either way."),
            D(67, MissionRole.Discovery, "I want the other half of that mask.",
                companions: C(Fumi), camp: CampId.Garrison, reads: S("duel_won"), sets: S("mask_half")),
            D(68, MissionRole.Downtime, "I want to hear him say it.",
                companions: C(Nire, Daigo, Tsuru, Suzu, Fumi), reads: S("mask_half"),
                sets: S("companions_refused"),
                consequence: "They refuse to attack tonight. Waiting until morning is a playable mission. Going alone is also playable, and worse."),
            D(69, MissionRole.Story, "I want to not become him.",
                companions: C(Nire), reads: S("companions_refused"), sets: S("nire_said_it"),
                consequence: "NIRE says it before Jin does, with the standing of somebody who knew both men. Renzo does not answer her."),
            D(70, MissionRole.Consequence, "I want the mountain fortress.",
                reads: S("nire_said_it"), sets: S("jin_dead"),
                consequence: "Jin dies. The garrison town stands down and its people will talk to anyone, including Renzo."),

            // ===== CHAPTER 8 — THE IRON FORTRESS. One place, five missions,
            // and the mission where somebody else gets what Renzo came for.
            D(71, MissionRole.Personal, "I want to know what is killing his men.",
                companions: C(Nire), camp: CampId.FrozenCamp, reads: S("jin_dead"),
                sets: S("nire_stayed", "frozen_seen"),
                consequence: "NIRE will not climb. She waits at the reed village, and the medicine stops being free."),
            D(72, MissionRole.Prepare, "I want that slope on my side.",
                A(Burn, Quiet, Front), companions: C(Suzu, Tsuru), camp: CampId.FrozenCamp,
                reads: S("frozen_seen"), sets: S("magazine_blown"),
                consequence: "The guns are gone and the cornice above the road is loose. It comes down at 73 whether Renzo is ready or not."),
            D(73, MissionRole.Story, "I want the wall out of the white.",
                companions: C(Daigo), camp: CampId.FrozenCamp, reads: S("magazine_blown")),
            D(74, MissionRole.Assault, "I want to know whose steel that is.",
                A(Front, Ambush, Allied), approachFlag: "frozen_seen",
                companions: C(Daigo, Tsuru, Toku), camp: CampId.IronFortress,
                reads: S("magazine_blown", "kiba_held"), sets: S("wall_plans", "tsuru_wall"),
                consequence: "TSURU takes the wall he deserted. He does not enjoy it, and the game does not let the player enjoy it either."),
            D(75, MissionRole.Assault, "I want in without waking it.",
                A(Quiet, Front, Burn), approachFlag: "wall_plans",
                companions: C(Suzu), camp: CampId.IronFortress, reads: S("wall_plans"),
                sets: S("inside_fortress")),
            D(76, MissionRole.Personal, "I want the top cell.",
                A(Quiet, Front, Allied), companions: C(Suzu), camp: CampId.IronFortress,
                reads: S("inside_fortress"), sets: S("kanta_found"),
                consequence: "Every cell opens and none of them is Aiko. One of them is SUZU's brother. She gets what Renzo came for."),
            D(77, MissionRole.Discovery, "I want the order in his own hand.",
                A(Front, Quiet), companions: C(Fumi), camp: CampId.IronFortress,
                reads: S("kanta_found"), sets: S("suzu_left"),
                consequence: "SUZU walks her brother down the mountain. She is gone until 89, and whether she comes back depends on Kiba."),
            D(78, MissionRole.Personal, "I want him to unmake his own work.",
                A(Front, Ambush), companions: C(Toku, Daigo), camp: CampId.IronFortress,
                reads: S("kiba_forge", "suzu_left"), sets: S("iron_broken"),
                consequence: "TOKU stands in front of his own mark on their armour. Kagehira's steel line breaks."),
            D(79, MissionRole.Story, "I want the hall behind him.",
                companions: C(Daigo), camp: CampId.IronFortress, reads: S("iron_broken"),
                sets: S("hoshu_dead"),
                consequence: "Hoshu dies at the gate. The inner fortress loses its discipline; patrols wander for the rest of the chapter."),
            D(80, MissionRole.Consequence, "I want her.",
                reads: S("hoshu_dead"), sets: S("aiko_found"),
                consequence: "Aiko, standing, older, alive. Everything the campaign has been for, and forty missions of world-state now point at getting her out."),

            // ===== CHAPTER 9 — THE BLACK SEAL. The chapter where preparation
            // stops being a bonus and decides who is alive at the end of it.
            D(81, MissionRole.Downtime, "I want ten years of what happened to her.",
                reads: S("aiko_found"),
                consequence: "No enemies. The only mission in the game whose whole content is two people in a room."),
            D(82, MissionRole.Defend, "I want to hold this hall until she finishes.",
                A(Front, Allied), companions: C(Daigo), reads: S("aiko_found")),
            D(83, MissionRole.Story, "I want her out of this building.",
                A(Front, Allied, Ambush), companions: C(Daigo, Tsuru), camp: CampId.IronFortress,
                sets: S("aiko_out")),
            D(84, MissionRole.Assault, "I want the half of his army that is not his.",
                A(Allied, Front), companions: C(Daigo, Tsuru), reads: S("aiko_out", "kiba_held"),
                sets: S("mutineers_armed"),
                consequence: "Half the army stands down at the sight of Aiko. Whether they arm and stay is the difference at 89."),
            D(85, MissionRole.Story, "I want the door.",
                companions: C(Tsuru), reads: S("mutineers_armed")),
            D(86, MissionRole.Memory, "I want to hear him say it to me.", sets: S("father_heard")),
            D(87, MissionRole.Prepare, "I want to know what the lists are actually for.",
                A(Front, Quiet), companions: C(Fumi), reads: S("father_heard"),
                sets: S("walls_repaired"),
                consequence: "FUMI will not burn the records, and it costs the player something real. What she keeps rebuilds the walls."),
            D(88, MissionRole.Story, "I want to be the one who opens it.",
                companions: C(Daigo), reads: S("walls_repaired")),
            D(89, MissionRole.Defend, "I want to be at the door before he is.",
                A(Allied, Front), companions: C(Daigo, Tsuru, Suzu),
                reads: S("mutineers_armed", "walls_repaired", "kiba_held", "magazine_blown", "reed_trust"),
                sets: S("gate_held"),
                consequence: "The largest waves in the game. DAIGO holds the gate, and whether he walks away from it is decided by five flags set across fifty missions."),
            D(90, MissionRole.Consequence, "I want to stop him on the stair.",
                reads: S("gate_held"), sets: S("door_opened"),
                consequence: "The door opens with Aiko's blood on the key. Everything after this is a mountain and one man."),

            // ===== CHAPTER 10 — THE SERPENT'S END. The companions are taken
            // away one mission at a time, on purpose, so that mission 95 means
            // what missions 1 to 11 meant and the player feels the difference.
            D(91, MissionRole.Story, "I want her off this mountain.",
                companions: C(Daigo, Tsuru), reads: S("door_opened")),
            D(92, MissionRole.Personal, "I want the line broken from behind.",
                A(Allied, Front, Ambush), companions: C(Tsuru, Daigo, Suzu),
                reads: S("mutineers_armed", "gate_held"), sets: S("army_broken"),
                consequence: "TSURU puts names on the shafts. The mutineers fight for Aiko and the army fights for pay, and it shows."),
            D(93, MissionRole.Discovery, "I want the summit.",
                companions: C(Tsuru), camp: CampId.SummitRoad, reads: S("army_broken"),
                sets: S("alone_from_here"),
                consequence: "The companions stop here, every one of them for their own reason and all of them said out loud. Toku goes one post further, carrying something, and no further than that."),
            D(94, MissionRole.Personal, "I want a blade that is not theirs.",
                A(Front, Ambush), companions: C(Toku), camp: CampId.SummitRoad,
                reads: S("kiba_forge", "alone_from_here"), sets: S("last_blade"),
                consequence: "TOKU's last honest blade, made from Yorune steel taken back, handed over at the last guard post. He turns around there. The final duel is different with it."),
            D(95, MissionRole.Story, "I want her back.",
                camp: CampId.SummitRoad, reads: S("last_blade"), sets: S("came_alone"),
                consequence: "Aiko is taken. From here Renzo is alone, and the game has spent eighty missions earning that word."),
            D(96, MissionRole.Story, "I want the door at the top of the stair.",
                camp: CampId.SummitRoad, reads: S("came_alone")),
            D(97, MissionRole.Story, "I want to hear the rest of it."),
            D(98, MissionRole.Story, "I want to know why she has not run."),
            D(99, MissionRole.Consequence, "I want to not become him.",
                sets: S("kagehira_dead"),
                consequence: "The system collapses. The water is nobody's, and the mountain belongs to the villages that drink from it."),
            D(100, MissionRole.Downtime, "I want to build one.",
                companions: C(Suzu, Fumi, Tsuru, Daigo, Toku, Nire), village: VillageId.Yorune,
                reads: S("kagehira_dead", "gate_held", "kiba_held", "reed_trust", "ashfall_freed"),
                consequence: "No combat. Who is standing at Yorune at dawn is the sum of everything the player did and did not prepare for."),
        };

        public static MissionDesign For(int id) =>
            id >= 1 && id <= Designs.Length ? Designs[id - 1] : null;

        public static CompanionDef Def(Companion c)
        {
            foreach (var d in Companions) if (d.id == c) return d;
            return null;
        }

        public static CampDef Def(CampId c)
        {
            foreach (var d in Camps) if (d.id == c) return d;
            return null;
        }

        public static VillageDef Def(VillageId v)
        {
            foreach (var d in Villages) if (d.id == v) return d;
            return null;
        }

        /// <summary>Missions that belong to a camp, in the order they are played.</summary>
        public static int[] MissionsAt(CampId c) => Def(c)?.missions ?? System.Array.Empty<int>();
    }
}
