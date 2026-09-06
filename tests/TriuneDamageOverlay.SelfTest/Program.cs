using TriuneDamageOverlay.Core;

var parser = new EverQuestDamageParser("Atlaswar");
var cases = new (string Line, bool ShouldMatch, int Amount, DamageKind? Kind)[]
{
    ("[Sun Sep 06 12:00:00 2026] You slash a decaying skeleton for 123 points of damage.", true, 123, DamageKind.Slashing),
    ("[Sun Sep 06 12:00:01 2026] You hit a rat for 2,345 points of non-melee damage.", true, 2345, DamageKind.Melee),
    ("[Sun Sep 06 12:00:02 2026] Your Fireball hits a rat for 456 points of damage.", true, 456, DamageKind.Spell),
    ("[Sun Sep 06 12:00:03 2026] a rat has taken 78 damage from your Immolate.", true, 78, DamageKind.DamageOverTime),
    ("[Sun Sep 06 12:00:04 2026] You have scored a critical hit! (1,284)", true, 1284, DamageKind.Critical),
    ("[Sun Sep 06 12:00:05 2026] Atlaswar slashes a rat for 99 points of damage.", true, 99, DamageKind.Slashing),
    ("[Wed Feb 19 13:46:31 2025] You slash a brownie for 149586 points of damage. (Critical)", true, 149586, DamageKind.Critical),
    ("[Wed Feb 19 13:46:31 2025] You hit a brownie for 1973 points of magic damage by Overdrive Punch.", true, 1973, DamageKind.Proc),
    ("[Sun Sep 06 12:00:05 2026] You pierce a rat for 101 points of damage.", true, 101, DamageKind.Piercing),
    ("[Sun Sep 06 12:00:05 2026] You crush a rat for 102 points of damage.", true, 102, DamageKind.Crushing),
    ("[Sun Sep 06 12:00:05 2026] You kick a rat for 103 points of damage.", true, 103, DamageKind.Kick),
    ("[Sun Sep 06 12:00:05 2026] You bash a rat for 104 points of damage.", true, 104, DamageKind.Bash),
    ("[Sun Sep 06 12:00:05 2026] You backstab a rat for 105 points of damage.", true, 105, DamageKind.Backstab),
    ("[Sun Sep 06 12:00:05 2026] You shoot a rat for 106 points of damage.", true, 106, DamageKind.Ranged),
    ("[Thu May 12 19:08:54 2022] The Primal Vampire has taken 318273 damage from your Grip of Quietus Rk. II. (Critical)", true, 318273, DamageKind.Critical),
    ("[Sun Sep 06 12:00:06 2026] Otherplayer slashes a rat for 999 points of damage.", false, 0, null),
    ("[Sun Sep 06 12:00:07 2026] Fluffy hits a rat for 222 points of damage.", false, 0, null),
    ("[Sun Sep 06 12:00:08 2026] a rat hits YOU for 333 points of damage.", false, 0, null),
    ("[Sun Sep 06 12:00:09 2026] Otherplayer tells the group, 'You slash a rat for 444 points of damage.'", false, 0, null),
};

var parserFailed = 0;
foreach (var test in cases)
{
    var matched = parser.TryParse(test.Line, out var damage);
    var pass = matched == test.ShouldMatch && (!matched || (damage!.Amount == test.Amount && damage.Kind == test.Kind));
    Console.WriteLine($"{(pass ? "PASS" : "FAIL")}  {test.Line}");
    if (!pass) parserFailed++;
}

Console.WriteLine($"\n{cases.Length - parserFailed}/{cases.Length} parser checks passed.");

var alertMatcher = new CombatAlertMatcher();
var rampage = new CombatAlertRule("rampage", "goes on a RAMPAGE", "RAMPAGE! — {mob}");
var alertCases = new (string Line, CombatAlertRule Rule, bool ShouldMatch, string ExpectedText)[]
{
    ("[Sun Sep 06 12:10:00 2026] Emperor Ssraeshza goes on a RAMPAGE!", rampage, true, "RAMPAGE! — Emperor Ssraeshza"),
    ("[Sun Sep 06 12:10:01 2026] emperor ssraeshza GOES ON A RAMPAGE!", rampage, true, "RAMPAGE! — emperor ssraeshza"),
    ("[Sun Sep 06 12:10:02 2026] Ravaloft tells the group, 'Emp Ssra goes on a RAMPAGE!'", rampage, false, ""),
    ("[Sun Sep 06 12:10:03 2026] You say, 'goes on a RAMPAGE'", rampage, false, ""),
    ("[Sun Sep 06 12:10:04 2026] A serpent begins to glow with deadly energy.", rampage, false, ""),
    ("[Sun Sep 06 12:10:05 2026] A serpent begins to glow with deadly energy.", new CombatAlertRule("custom", "begins to glow with deadly energy", "MOVE! — {mob}"), true, "MOVE! — A serpent")
};
var alertFailed = 0;
foreach (var test in alertCases)
{
    var matched = alertMatcher.TryMatch(test.Line, test.Rule, out var alert);
    var pass = matched == test.ShouldMatch && (!matched || alert!.Text == test.ExpectedText);
    Console.WriteLine($"{(pass ? "PASS" : "FAIL")}  ALERT  {test.Line}");
    if (!pass) alertFailed++;
}

Console.WriteLine($"\n{alertCases.Length - alertFailed}/{alertCases.Length} alert checks passed.");
return parserFailed + alertFailed == 0 ? 0 : 1;
