using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Season;

namespace InfinityMercsApp.Views.Season;

public partial class DowntimePage : ContentPage, IQueryAttributable
{
    private string _companyFilePath = string.Empty;
    private string _seasonFilePath = string.Empty;
    private bool _eventResolved;
    private Action<int>? _pendingRollHandler;

    private readonly MissionDebrief _debrief;

    public DowntimePage(MissionDebrief debrief)
    {
        InitializeComponent();
        _debrief = debrief;

        var picks = new List<string>();
        for (var i = 1; i <= 20; i++) picks.Add(i.ToString());
        RealPicker.ItemsSource = picks;

        ArmPendingRoll("Press ROLL for a random event, or pick a value and press REAL.", StartEvent);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _companyFilePath = NavigationParameters.GetString(query, NavigationParameters.CompanyFilePath) ?? _companyFilePath;
        _seasonFilePath = NavigationParameters.GetString(query, NavigationParameters.SeasonFilePath) ?? _seasonFilePath;
    }

    // ── Bottom bar dispatch ──────────────────────────────────────────────────

    private void OnRollClicked(object sender, EventArgs e)
        => DispatchRoll(Random.Shared.Next(1, 21));

    private void OnRealClicked(object sender, EventArgs e)
    {
        if (RealPicker.SelectedIndex < 0)
        {
            DisplayAlertAsync("Pick a number", "Choose a value 1-20 first.", "OK");
            return;
        }
        DispatchRoll(RealPicker.SelectedIndex + 1);
    }

    private void DispatchRoll(int roll)
    {
        if (_pendingRollHandler is null) return;
        var handler = _pendingRollHandler;
        _pendingRollHandler = null;
        SetBarEnabled(false);
        handler(roll);
    }

    private void ArmPendingRoll(string label, Action<int> handler)
    {
        _pendingRollHandler = handler;
        RollContextLabel.Text = label;
        SetBarEnabled(true);
    }

    private void DisarmPendingRoll(string label)
    {
        _pendingRollHandler = null;
        RollContextLabel.Text = label;
        SetBarEnabled(false);
    }

    private void SetBarEnabled(bool enabled)
    {
        RollButton.IsEnabled = enabled;
        RealButton.IsEnabled = enabled;
        RealPicker.IsEnabled = enabled;
        RollBar.Opacity = enabled ? 1.0 : 0.45;
    }

    private async void OnBackToBaseClicked(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PopToRootAsync();
    }

    // ── Event flow ───────────────────────────────────────────────────────────

    private void StartEvent(int roll)
    {
        EmptyLabel.IsVisible = false;
        EventStack.Children.Clear();

        var evt = DowntimeEventTable.LookupByRoll(roll);
        EventStack.Children.Add(BuildRollHeader(roll, evt));

        _ = SeasonFileService.UpdateLatestRoundAsync(_seasonFilePath, round =>
        {
            round.Downtime.EventId = evt.RollRange;
            round.Downtime.Result = evt.Description;
        });

        if (evt.IsNoIncident)
        {
            EventStack.Children.Add(new Label
            {
                Text = evt.Description,
                TextColor = Color.FromArgb("#B5C0CE"),
                FontSize = 14,
                Margin = new Thickness(0, 8, 0, 4)
            });
            ScrollToBottom();
            EventStack.Children.Add(new Label
            {
                Text = "SELECT AN EVENT",
                TextColor = Color.FromArgb("#8A97A8"),
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 4, 0, 6)
            });
            DisarmPendingRoll("Select an event or choose No Event below.");

            foreach (var option in DowntimeEventTable.AllEvents)
            {
                var capturedOption = option;
                var row = BuildEventPickRow(option, () =>
                {
                    // Clear the picker section and run the chosen event
                    EventStack.Children.Clear();
                    ArmPendingRoll("Press ROLL for a random event, or pick a value and press REAL.", StartEvent);
                    StartEvent(capturedOption.RepresentativeRoll);
                });
                EventStack.Children.Add(row);
            }

            var noEventBtn = new Button
            {
                Text = "NO EVENT",
                BackgroundColor = Color.FromArgb("#3A4554"),
                TextColor = Color.FromArgb("#8A97A8"),
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 6,
                HeightRequest = 40,
                Margin = new Thickness(0, 8, 0, 0)
            };
            noEventBtn.Clicked += (_, _) =>
            {
                noEventBtn.IsEnabled = false;
                // Grey out remaining rows
                foreach (var child in EventStack.Children.OfType<VisualElement>())
                    if (!ReferenceEquals(child, noEventBtn)) child.Opacity = 0.35;
                DisarmPendingRoll("No event chosen.");

                _ = SeasonFileService.UpdateLatestRoundAsync(_seasonFilePath, round =>
                {
                    round.Downtime.ChosenPlan = "No event";
                    round.Downtime.Result = "No event chosen";
                });

                ResolveEvent();
            };
            EventStack.Children.Add(noEventBtn);
            ScrollToBottom();
            return;
        }

        EventStack.Children.Add(new Label
        {
            Text = evt.Description,
            TextColor = Color.FromArgb("#B5C0CE"),
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 8)
        });

        if (evt.EventTraits.Count > 0)
        {
            EventStack.Children.Add(new Label
            {
                Text = "Event Traits: " + string.Join(", ", evt.EventTraits),
                TextColor = Color.FromArgb("#F59E0B"),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold
            });
        }

        // Add a card for each of the three choices
        var choiceCards = new List<Border>();
        for (var i = 0; i < evt.Choices.Count; i++)
        {
            var idx = i;
            var card = BuildChoiceCard(evt, evt.Choices[i], i + 1, () => OnChoiceSelected(evt, idx, choiceCards));
            choiceCards.Add(card);
            EventStack.Children.Add(card);
        }

        DisarmPendingRoll("Pick a choice above to continue.");
        ScrollToBottom();
    }

    private static Border BuildEventPickRow(DowntimeEvent option, Action onPicked)
    {
        var label = new Label
        {
            Text = $"{option.RollRange}  —  {option.Description}",
            TextColor = Color.FromArgb("#E6EBF2"),
            FontSize = 13,
            LineBreakMode = LineBreakMode.WordWrap
        };

        var border = new Border
        {
            BackgroundColor = Color.FromArgb("#161B22"),
            Stroke = Color.FromArgb("#3A4554"),
            StrokeThickness = 1,
            Padding = new Thickness(12, 10, 12, 10),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
            Content = label
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            border.GestureRecognizers.Remove(tap);
            onPicked();
        };
        border.GestureRecognizers.Add(tap);
        return border;
    }

    private View BuildRollHeader(int roll, DowntimeEvent evt)
    {
        var border = new Border
        {
            BackgroundColor = Color.FromArgb("#161B22"),
            Stroke = Color.FromArgb("#34D399"),
            StrokeThickness = 1,
            Padding = new Thickness(12, 8, 12, 8),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 }
        };
        border.Content = new Label
        {
            Text = $"Rolled {roll}  →  Event {evt.RollRange}",
            TextColor = Color.FromArgb("#E6EBF2"),
            FontAttributes = FontAttributes.Bold,
            FontSize = 15,
            HorizontalTextAlignment = TextAlignment.Center
        };
        return border;
    }

    private Border BuildChoiceCard(DowntimeEvent evt, DowntimeChoice choice, int choiceNumber, Action onPicked)
    {
        var stack = new VerticalStackLayout { Spacing = 6, Padding = new Thickness(12) };

        var header = new Label
        {
            Text = $"CHOICE {choiceNumber}{(string.IsNullOrEmpty(choice.TestLabel) ? "" : $"  ({choice.TestLabel})")}",
            TextColor = Color.FromArgb("#8A97A8"),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold
        };
        stack.Children.Add(header);

        stack.Children.Add(new Label
        {
            Text = choice.Description,
            TextColor = Color.FromArgb("#E6EBF2"),
            FontSize = 14
        });

        if (choice.Traits.Count > 0)
        {
            stack.Children.Add(new Label
            {
                Text = "Traits: " + string.Join(", ", choice.Traits.Select(t => t.Display)),
                TextColor = Color.FromArgb("#F59E0B"),
                FontSize = 12,
                FontAttributes = FontAttributes.Bold
            });
        }

        var pickBtn = new Button
        {
            Text = "PICK",
            BackgroundColor = Color.FromArgb("#3A4554"),
            TextColor = Color.FromArgb("#E6EBF2"),
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 6,
            HeightRequest = 36,
            Margin = new Thickness(0, 4, 0, 0)
        };
        pickBtn.Clicked += (_, _) =>
        {
            pickBtn.IsEnabled = false;
            onPicked();
        };
        stack.Children.Add(pickBtn);

        var border = new Border
        {
            BackgroundColor = Color.FromArgb("#161B22"),
            Stroke = Color.FromArgb("#3A4554"),
            StrokeThickness = 1,
            Padding = new Thickness(0),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 }
        };
        border.Content = stack;
        return border;
    }

    private void OnChoiceSelected(DowntimeEvent evt, int choiceIndex, List<Border> choiceCards)
    {
        for (var i = 0; i < choiceCards.Count; i++)
        {
            if (i != choiceIndex)
                EventStack.Children.Remove(choiceCards[i]);
        }

        var choice = evt.Choices[choiceIndex];

        _ = SeasonFileService.UpdateLatestRoundAsync(_seasonFilePath, round =>
        {
            round.Downtime.ChosenPlan = $"Choice {choiceIndex + 1}: {choice.Description}";
        });

        EventStack.Children.Add(BuildResolutionSection(evt, choice));
        ScrollToBottom();
    }


    private View BuildResolutionSection(DowntimeEvent evt, DowntimeChoice choice)
    {
        var stack = new VerticalStackLayout { Spacing = 10, Margin = new Thickness(0, 8, 0, 0) };

        stack.Children.Add(new Label
        {
            Text = "RESOLUTION",
            TextColor = Color.FromArgb("#8A97A8"),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold
        });

        // Step 1: Determine the participant who performs the test, if applicable
        var participantArea = new VerticalStackLayout { Spacing = 6 };
        stack.Children.Add(participantArea);

        // Step 2: Pass/Fail (when applicable)
        var rollArea = new VerticalStackLayout { Spacing = 6 };
        stack.Children.Add(rollArea);

        // Step 3: Outcomes
        var outcomeArea = new VerticalStackLayout { Spacing = 6 };
        stack.Children.Add(outcomeArea);

        var requiredParticipant = ResolveRequiredParticipantKind(evt, choice);
        var availableUnits = _debrief.Units;

        ExperienceUnitResult? selectedParticipant = null;
        var crSpentTotal = 0;

        void ShowOutcomes(PassFailOutcome outcome)
        {
            outcomeArea.Children.Clear();

            outcomeArea.Children.Add(new Label
            {
                Text = $"OUTCOME: {OutcomeLabel(outcome)}",
                TextColor = outcome switch
                {
                    PassFailOutcome.Fail => Color.FromArgb("#F87171"),
                    PassFailOutcome.Pass => Color.FromArgb("#34D399"),
                    PassFailOutcome.CritPass => Color.FromArgb("#FBBF24"),
                    _ => Color.FromArgb("#E6EBF2")
                },
                FontAttributes = FontAttributes.Bold,
                FontSize = 14
            });

            var wonItems = new List<SeasonInventoryItem>();
            var consequences = BuildConsequenceLines(choice, outcome, selectedParticipant, wonItems);
            if (consequences.Count == 0)
            {
                outcomeArea.Children.Add(new Label
                {
                    Text = "No mechanical effect.",
                    TextColor = Color.FromArgb("#8A97A8"),
                    FontSize = 13
                });
            }
            else
            {
                foreach (var line in consequences)
                {
                    outcomeArea.Children.Add(new Label
                    {
                        Text = "• " + line,
                        TextColor = Color.FromArgb("#B5C0CE"),
                        FontSize = 13
                    });
                }
            }

            ApplyDataMutations(choice, outcome, selectedParticipant);

            var totals = ComputeDowntimeTotals(choice, outcome, crSpentTotal);
            var participantName = selectedParticipant?.Name;
            _ = SeasonFileService.UpdateLatestRoundAsync(_seasonFilePath, round =>
            {
                round.Downtime.Result = $"{OutcomeLabel(outcome)}{(participantName is null ? "" : $" — {participantName}")}";
                round.Downtime.ParticipantName = participantName ?? string.Empty;
                round.Downtime.CrGain = totals.CrGain;
                round.Downtime.NotorietyGain = totals.NotorietyGain;
                round.Downtime.XpGain = totals.XpGain;
                round.Downtime.SpentCr = totals.SpentCr;
                round.Downtime.SwcGain = totals.SwcGain;
                round.Downtime.OtherEffects = string.Join("; ", totals.OtherEffects);
                round.Downtime.WonItems = wonItems;
            });

            ScrollToBottom();
            ResolveEvent();
        }

        void ShowRollControls(int crSpent)
        {
            crSpentTotal = crSpent;
            rollArea.Children.Clear();

            var test = choice.Test;
            if (test is null)
            {
                ShowOutcomes(PassFailOutcome.Pass);
                return;
            }

            var baseTarget = ResolveTestTarget(test, selectedParticipant);
            if (!baseTarget.HasValue)
            {
                rollArea.Children.Add(new Label
                {
                    Text = $"Test: {test.Display} — target could not be resolved.",
                    TextColor = Color.FromArgb("#F87171"),
                    FontSize = 13
                });
                DisarmPendingRoll("Unable to resolve test target.");
                return;
            }

            var effectiveTarget = baseTarget.Value + crSpent;
            var critHint = effectiveTarget > 20
                ? $"  (crit on 20 and 1–{effectiveTarget - 20})"
                : string.Empty;

            rollArea.Children.Add(new Label
            {
                Text = $"Test: {test.Display}  target {effectiveTarget}{critHint}",
                TextColor = Color.FromArgb("#B5C0CE"),
                FontSize = 13
            });

            ArmPendingRoll(
                $"Roll for {test.Display} — target {effectiveTarget}.",
                rolled =>
                {
                    var outcome = ClassifyRoll(rolled, effectiveTarget);
                    rollArea.Children.Add(new Label
                    {
                        Text = $"Rolled: {rolled} vs {effectiveTarget}",
                        TextColor = Color.FromArgb("#E6EBF2"),
                        FontSize = 13,
                        FontAttributes = FontAttributes.Bold
                    });
                    ScrollToBottom();
                    ShowOutcomes(outcome);
                });
            ScrollToBottom();
        }

        void ShowP2PControls(int baseTarget)
        {
            rollArea.Children.Clear();

            rollArea.Children.Add(new Label
            {
                Text = "P2P — Spend CR to boost target (optional):",
                TextColor = Color.FromArgb("#B5C0CE"),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold
            });

            var targetLabel = new Label
            {
                Text = $"Target: {baseTarget}",
                TextColor = Color.FromArgb("#34D399"),
                FontSize = 14,
                FontAttributes = FontAttributes.Bold
            };

            var stepper = new Stepper
            {
                Minimum = 0,
                Maximum = 999,
                Increment = 1,
                Value = 0,
                HorizontalOptions = LayoutOptions.Fill
            };

            stepper.ValueChanged += (_, e) =>
            {
                var cr = (int)e.NewValue;
                var effective = baseTarget + cr;
                targetLabel.Text = effective > 20
                    ? $"Target: {effective}  (crit on 20 and 1–{effective - 20})"
                    : $"Target: {effective}";
            };

            var stepperRow = new Grid { ColumnDefinitions = new ColumnDefinitionCollection(
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }), ColumnSpacing = 8 };

            var crLabel = new Label
            {
                Text = "CR to spend:",
                TextColor = Color.FromArgb("#8A97A8"),
                FontSize = 13,
                VerticalTextAlignment = TextAlignment.Center
            };
            Grid.SetColumn(crLabel, 0);
            Grid.SetColumn(stepper, 1);
            Grid.SetColumn(targetLabel, 2);
            stepperRow.Children.Add(crLabel);
            stepperRow.Children.Add(stepper);
            stepperRow.Children.Add(targetLabel);
            rollArea.Children.Add(stepperRow);

            var confirmBtn = new Button
            {
                Text = "CONFIRM SPEND",
                BackgroundColor = Color.FromArgb("#3A4554"),
                TextColor = Color.FromArgb("#E6EBF2"),
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 6,
                HeightRequest = 36
            };
            confirmBtn.Clicked += (_, _) =>
            {
                var cr = (int)stepper.Value;
                stepper.IsEnabled = false;
                confirmBtn.IsEnabled = false;
                confirmBtn.Opacity = 0.45;
                ShowRollControls(cr);
            };
            rollArea.Children.Add(confirmBtn);
            ScrollToBottom();
        }

        void OnParticipantPicked(ExperienceUnitResult unit)
        {
            selectedParticipant = unit;
            participantArea.Children.Add(new Label
            {
                Text = $"Performed by: {unit.Name}",
                TextColor = Color.FromArgb("#34D399"),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold
            });
            ScrollToBottom();

            var hasP2P = choice.Traits.Any(t => t.Kind == TraitKind.P2P);
            if (hasP2P)
            {
                var test = choice.Test;
                var baseTarget = test is not null ? ResolveTestTarget(test, unit) ?? 0 : 0;
                ShowP2PControls(baseTarget);
            }
            else
            {
                ShowRollControls(0);
            }
        }

        var requirements = choice.Traits
            .Where(t => t.Kind == TraitKind.Requirement && !string.IsNullOrWhiteSpace(t.Detail))
            .Select(t => t.Detail!)
            .ToList();
        BuildParticipantSelector(participantArea, requiredParticipant, availableUnits, requirements, choice.Test, OnParticipantPicked);

        return stack;
    }

    // ── Participant selection ────────────────────────────────────────────────

    private static ParticipantKind ResolveRequiredParticipantKind(DowntimeEvent evt, DowntimeChoice choice)
    {
        // Event-level participants override choice. Event 13-14 = Renowned; some events specify "Merc" in text.
        if (evt.RequiredParticipant != ParticipantKind.None)
            return evt.RequiredParticipant;

        // A choice tagged with Captain must be performed by the Captain (Lieutenant).
        if (choice.Traits.Any(t => t.Kind == TraitKind.Captain))
            return ParticipantKind.Captain;

        // A merc always performs the event.
        return ParticipantKind.AnyMerc;
    }

    private void BuildParticipantSelector(
        VerticalStackLayout area,
        ParticipantKind kind,
        IReadOnlyList<ExperienceUnitResult> units,
        IReadOnlyList<string> requirements,
        DowntimeTest? test,
        Action<ExperienceUnitResult> onPicked)
    {
        area.Children.Add(new Label
        {
            Text = $"Select {ParticipantKindLabel(kind)}:",
            TextColor = Color.FromArgb("#B5C0CE"),
            FontSize = 13,
            FontAttributes = FontAttributes.Bold
        });

        var candidates = FilterCandidates(units, kind);
        if (candidates.Count == 0)
        {
            area.Children.Add(new Label
            {
                Text = "No eligible mercs found.",
                TextColor = Color.FromArgb("#F87171"),
                FontSize = 13
            });
            return;
        }

        var rowList = new VerticalStackLayout { Spacing = 6 };
        area.Children.Add(rowList);
        ScrollToBottom();

        var rowControls = new List<(Border Border, TapGestureRecognizer Tap)>();

        foreach (var unit in candidates)
        {
            var missing = requirements.Where(r => !UnitHasTrait(unit, r)).ToList();
            var eligible = missing.Count == 0;

            var nameLabel = new Label
            {
                Text = kind == ParticipantKind.Renowned ? $"{unit.Name}  ({unit.Renown} renown)" : unit.Name,
                TextColor = Color.FromArgb("#E6EBF2"),
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                VerticalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.TailTruncation
            };

            var missingLabel = new Label
            {
                Text = missing.Count > 0 ? $"Missing: {string.Join(", ", missing)}" : string.Empty,
                TextColor = Color.FromArgb("#F87171"),
                FontAttributes = FontAttributes.Bold,
                FontSize = 13,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.End,
                IsVisible = missing.Count > 0
            };

            var target = test is not null ? ResolveTestTarget(test, unit) : null;
            var targetLabel = new Label
            {
                Text = target.HasValue ? $"Target {target.Value}" : string.Empty,
                TextColor = Color.FromArgb("#34D399"),
                FontAttributes = FontAttributes.Bold,
                FontSize = 13,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.End,
                IsVisible = target.HasValue && missing.Count == 0
            };

            var rowGrid = new Grid { ColumnSpacing = 12 };
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(nameLabel, 0);
            Grid.SetColumn(missingLabel, 1);
            Grid.SetColumn(targetLabel, 1);
            rowGrid.Children.Add(nameLabel);
            rowGrid.Children.Add(missingLabel);
            rowGrid.Children.Add(targetLabel);

            var border = new Border
            {
                BackgroundColor = Color.FromArgb("#3A4554"),
                Stroke = Color.FromArgb("#3A4554"),
                StrokeThickness = 1,
                Padding = new Thickness(12, 10, 12, 10),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
                Opacity = eligible ? 1.0 : 0.45,
                Content = rowGrid
            };

            var tap = new TapGestureRecognizer();
            if (eligible)
            {
                var capturedUnit = unit;
                tap.Tapped += (_, _) =>
                {
                    // Disable every row after one is picked
                    foreach (var (b, t) in rowControls)
                    {
                        b.GestureRecognizers.Remove(t);
                        if (!ReferenceEquals(b, border)) b.Opacity = 0.45;
                    }
                    border.Stroke = Color.FromArgb("#34D399");
                    border.StrokeThickness = 2;
                    onPicked(capturedUnit);
                };
                border.GestureRecognizers.Add(tap);
            }

            rowControls.Add((border, tap));
            rowList.Children.Add(border);
        }
    }

    private static bool UnitHasTrait(ExperienceUnitResult unit, string requirement)
    {
        if (string.IsNullOrWhiteSpace(requirement)) return true;
        var haystack = $"{unit.Skills} {unit.Equipment}";
        return haystack.Contains(requirement, StringComparison.OrdinalIgnoreCase);
    }

    private static List<ExperienceUnitResult> FilterCandidates(IReadOnlyList<ExperienceUnitResult> units, ParticipantKind kind)
    {
        switch (kind)
        {
            case ParticipantKind.Mvp:
                return units.Where(u => u.IsMvp).ToList();
            case ParticipantKind.Captain:
                return units.Where(u => u.IsLieutenant).ToList();
            case ParticipantKind.Renowned:
                if (units.Count == 0) return new List<ExperienceUnitResult>();
                var topRenown = units.Max(u => u.Renown);
                return units.Where(u => u.Renown == topRenown).ToList();
            default:
                return units.ToList();
        }
    }

    private static string ParticipantKindLabel(ParticipantKind kind) => kind switch
    {
        ParticipantKind.Mvp => "MVP (from last contract)",
        ParticipantKind.Captain => "the Captain",
        ParticipantKind.Renowned => "the Most Renowned merc",
        _ => "a Merc"
    };

    // ── Stat-test target resolution ──────────────────────────────────────────

    private static int? ResolveTestTarget(DowntimeTest test, ExperienceUnitResult? unit)
    {
        switch (test.Kind)
        {
            case TestKind.FixedPs10:
                return 10;
            case TestKind.FixedPs10PlusArm:
            {
                if (unit is null) return 10;
                return 10 + ParseStat(unit.UnitArm);
            }
            case TestKind.Stat:
            {
                if (unit is null) return null;
                var raw = test.Stat switch
                {
                    Stat.Ph => unit.UnitPh,
                    Stat.Bs => unit.UnitBs,
                    Stat.Cc => unit.UnitCc,
                    Stat.Wip => unit.UnitWip,
                    _ => "-"
                };
                if (!int.TryParse(raw, out var n)) return null;
                return n + test.Modifier;
            }
            default:
                return null;
        }
    }

    private static int ParseStat(string s) => int.TryParse(s, out var n) ? n : 0;

    private static PassFailOutcome ClassifyRoll(int roll, int target)
    {
        if (target <= 20)
        {
            if (roll == target) return PassFailOutcome.CritPass;
            if (roll < target) return PassFailOutcome.Pass;
            return PassFailOutcome.Fail;
        }
        // Target > 20: all rolls succeed. Crits are roll == 20 OR roll <= (target - 20).
        var critRange = target - 20;
        if (roll == 20 || roll <= critRange) return PassFailOutcome.CritPass;
        return PassFailOutcome.Pass;
    }

    private static string OutcomeLabel(PassFailOutcome o) => o switch
    {
        PassFailOutcome.Fail => "FAIL",
        PassFailOutcome.Pass => "PASS",
        PassFailOutcome.CritPass => "CRITICAL PASS",
        _ => "—"
    };

    // ── Consequences ─────────────────────────────────────────────────────────

    private static List<string> BuildConsequenceLines(
        DowntimeChoice choice,
        PassFailOutcome outcome,
        ExperienceUnitResult? participant,
        List<SeasonInventoryItem> wonItems)
    {
        var lines = new List<string>();
        var participantName = participant?.Name ?? "selected merc";

        foreach (var trait in choice.Traits)
        {
            // (Neg) means only fail-side of the paired trait counts.
            if (trait.Negated && outcome != PassFailOutcome.Fail) continue;

            switch (trait.Kind)
            {
                case TraitKind.Chaotic:
                    lines.Add(outcome switch
                    {
                        PassFailOutcome.Fail => "Lose 1 Notoriety",
                        PassFailOutcome.Pass => "Gain 1 Notoriety",
                        PassFailOutcome.CritPass => "Gain 2 Notoriety",
                        _ => ""
                    });
                    break;
                case TraitKind.Lawful:
                    lines.Add(outcome switch
                    {
                        PassFailOutcome.Fail => "Gain 1 Notoriety",
                        PassFailOutcome.Pass => "Lose 1 Notoriety",
                        PassFailOutcome.CritPass => "Lose 2 Notoriety",
                        _ => ""
                    });
                    break;
                case TraitKind.Attack:
                    if (outcome == PassFailOutcome.Fail)
                        lines.Add($"Injury inflicted on {participantName}");
                    break;
                case TraitKind.Cr:
                    lines.Add(outcome switch
                    {
                        PassFailOutcome.Fail => "Lose 5 CR",
                        PassFailOutcome.Pass => "Gain 5 CR",
                        PassFailOutcome.CritPass => "Gain 6 CR",
                        _ => ""
                    });
                    break;
                case TraitKind.Xp:
                    lines.Add(outcome switch
                    {
                        PassFailOutcome.Pass => $"{participantName} gains 2 XP",
                        PassFailOutcome.CritPass => $"{participantName} gains 3 XP",
                        _ => ""
                    });
                    break;
                case TraitKind.Weapon:
                    if (outcome is PassFailOutcome.Pass or PassFailOutcome.CritPass)
                    {
                        var pistol = RollPistol(outcome == PassFailOutcome.CritPass ? 1 : 0);
                        var prefix = outcome == PassFailOutcome.CritPass
                            ? "Roll a random pistol (+1 SD)"
                            : "Roll a random pistol";
                        lines.Add($"{prefix}: {pistol.Display}");
                        wonItems.Add(new SeasonInventoryItem
                        {
                            Name = pistol.Name,
                            Category = GearSlot.Sidearm.Key(),
                            Source = "Downtime"
                        });
                    }
                    break;
                case TraitKind.Swc:
                    if (outcome is PassFailOutcome.Pass or PassFailOutcome.CritPass)
                        lines.Add("Gain 0.5 SWC");
                    break;
                case TraitKind.P2P:
                    lines.Add(outcome switch
                    {
                        PassFailOutcome.Fail => "Spent CR are lost; SR not increased",
                        PassFailOutcome.Pass => "Spent CR are lost; SR increased by 1 per CR spent",
                        PassFailOutcome.CritPass => "Spent CR are kept; SR increased by 1 per CR spent",
                        _ => ""
                    });
                    break;
                case TraitKind.Skill:
                    if (outcome is PassFailOutcome.Pass or PassFailOutcome.CritPass)
                        lines.Add($"{participantName} gains skill: {trait.Detail ?? "(see event)"}");
                    break;
                case TraitKind.Lt:
                    if (outcome is PassFailOutcome.Pass or PassFailOutcome.CritPass)
                        lines.Add("You may select any member of your team to be the new Captain");
                    break;
                case TraitKind.Captain:
                    lines.Add("Captain participation required (see event)");
                    break;
                case TraitKind.Requirement:
                    lines.Add($"Requirement: {trait.Detail ?? "(see event)"}");
                    break;
                case TraitKind.Note:
                    if (!string.IsNullOrWhiteSpace(trait.Detail)) lines.Add(trait.Detail!);
                    break;
            }
        }

        return lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
    }

    private sealed record PistolRoll(string Name, int Roll, int SdBonus)
    {
        public string Display => $"{Name}{(SdBonus > 0 ? $" (+{SdBonus} SD)" : "")} (rolled {Roll})";
    }

    private static PistolRoll RollPistol(int sdBonus)
    {
        var roll = Random.Shared.Next(1, 21);
        return new PistolRoll(SeasonRollTables.Pistols.Lookup(roll)!, roll, sdBonus);
    }

    // ── Downtime totals (persistence) ────────────────────────────────────────

    private readonly record struct DowntimeTotals(int CrGain, int NotorietyGain, int XpGain, int SpentCr, double SwcGain, List<string> OtherEffects);

    private static DowntimeTotals ComputeDowntimeTotals(DowntimeChoice choice, PassFailOutcome outcome, int crSpent)
    {
        var crGain = 0;
        var notorietyGain = 0;
        var xpGain = 0;
        var spentCr = 0;
        var swcGain = 0.0;
        var other = new List<string>();

        foreach (var trait in choice.Traits)
        {
            if (trait.Negated && outcome != PassFailOutcome.Fail) continue;

            switch (trait.Kind)
            {
                case TraitKind.Chaotic:
                    notorietyGain += outcome switch
                    {
                        PassFailOutcome.Fail => -1,
                        PassFailOutcome.Pass => 1,
                        PassFailOutcome.CritPass => 2,
                        _ => 0
                    };
                    break;
                case TraitKind.Lawful:
                    notorietyGain += outcome switch
                    {
                        PassFailOutcome.Fail => 1,
                        PassFailOutcome.Pass => -1,
                        PassFailOutcome.CritPass => -2,
                        _ => 0
                    };
                    break;
                case TraitKind.Cr:
                    crGain += outcome switch
                    {
                        PassFailOutcome.Fail => -5,
                        PassFailOutcome.Pass => 5,
                        PassFailOutcome.CritPass => 6,
                        _ => 0
                    };
                    break;
                case TraitKind.Xp:
                    xpGain += outcome switch
                    {
                        PassFailOutcome.Pass => 2,
                        PassFailOutcome.CritPass => 3,
                        _ => 0
                    };
                    break;
                case TraitKind.P2P:
                    // Pass/CritPass keeps or spends CR depending on outcome; the SR/CR rule is handled
                    // in BuildConsequenceLines. Persist the spend separately.
                    if (outcome == PassFailOutcome.CritPass)
                    {
                        // Spent CR is kept on a crit pass — no net loss.
                    }
                    else
                    {
                        spentCr += crSpent;
                    }
                    break;
                case TraitKind.Weapon:
                    if (outcome is PassFailOutcome.Pass or PassFailOutcome.CritPass)
                        other.Add(outcome == PassFailOutcome.CritPass ? "Random pistol (+1 SD)" : "Random pistol");
                    break;
                case TraitKind.Skill:
                    if (outcome is PassFailOutcome.Pass or PassFailOutcome.CritPass)
                        other.Add($"Skill awarded: {trait.Detail ?? "(see event)"}");
                    break;
                case TraitKind.Lt:
                    if (outcome is PassFailOutcome.Pass or PassFailOutcome.CritPass)
                        other.Add("New Captain may be selected");
                    break;
                case TraitKind.Attack:
                    if (outcome == PassFailOutcome.Fail)
                        other.Add("Participant injured");
                    break;
                case TraitKind.Swc:
                    if (outcome is PassFailOutcome.Pass or PassFailOutcome.CritPass)
                        swcGain += 0.5;
                    break;
                case TraitKind.Note:
                    if (!string.IsNullOrWhiteSpace(trait.Detail)) other.Add(trait.Detail!);
                    break;
            }
        }

        return new DowntimeTotals(crGain, notorietyGain, xpGain, spentCr, swcGain, other);
    }

    // ── Data mutations ───────────────────────────────────────────────────────

    private static void ApplyDataMutations(DowntimeChoice choice, PassFailOutcome outcome, ExperienceUnitResult? participant)
    {
        if (participant is null) return;

        foreach (var trait in choice.Traits)
        {
            if (trait.Negated && outcome != PassFailOutcome.Fail) continue;

            if (trait.Kind == TraitKind.Xp)
            {
                participant.XpData.DowntimeBonusXp += outcome switch
                {
                    PassFailOutcome.Pass => 2,
                    PassFailOutcome.CritPass => 3,
                    _ => 0
                };
            }
            else if (trait.Kind == TraitKind.Attack && outcome == PassFailOutcome.Fail)
            {
                participant.GainedInjury = true;
            }
        }
    }

    // ── Event resolution / footer ────────────────────────────────────────────

    private void ScrollToBottom() =>
        Dispatcher.Dispatch(async () =>
            await EventScrollView.ScrollToAsync(0, double.MaxValue, animated: true));

    private void ResolveEvent()
    {
        _eventResolved = true;
        _pendingRollHandler = null;
        RollBar.IsVisible = false;
        RollContextLabel.IsVisible = false;
        ContinueButton.IsVisible = true;
    }
}
