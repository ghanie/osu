﻿// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Tools;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Tests.Beatmaps;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Game.Tests.Visual
{
    public class TestCaseEditorSeekSnapping : OsuTestCase
    {
        public override IReadOnlyList<Type> RequiredTypes => new[] { typeof(HitObjectComposer) };

        private Track track;
        private HitObjectComposer composer;

        [BackgroundDependencyLoader]
        private void load(OsuGameBase osuGame)
        {
            var testBeatmap = new Beatmap
            {
                ControlPointInfo = new ControlPointInfo
                {
                    TimingPoints =
                    {
                        new TimingControlPoint { Time = 0, BeatLength = 200},
                        new TimingControlPoint { Time = 100, BeatLength = 400 },
                        new TimingControlPoint { Time = 175, BeatLength = 800 },
                        new TimingControlPoint { Time = 350, BeatLength = 200 },
                        new TimingControlPoint { Time = 450, BeatLength = 100 }
                    }
                },
                HitObjects =
                {
                    new HitCircle { StartTime = 0 },
                    new HitCircle { StartTime = 5000 }
                }
            };

            osuGame.Beatmap.Value = new TestWorkingBeatmap(testBeatmap);
            track = osuGame.Beatmap.Value.Track;

            Child = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                Content = new[]
                {
                    new Drawable[] { composer = new TestHitObjectComposer(new OsuRuleset()) },
                    new Drawable[] { new TimingPointVisualiser(testBeatmap, track) },
                },
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.Distributed),
                    new Dimension(GridSizeMode.AutoSize),
                }
            };

//            testSeekNoSnapping();
//            testSeekSnappingOnBeat();
//            testSeekSnappingInBetweenBeat();
//            testSeekForwardNoSnapping();
//            testSeekForwardSnappingOnBeat();
            testSeekForwardSnappingFromInBetweenBeat();
        }

        /// <summary>
        /// Tests whether time is correctly seeked without snapping.
        /// </summary>
        private void testSeekNoSnapping()
        {
            reset();

            // Forwards
            AddStep("Seek(0)", () => composer.SeekTo(0));
            AddAssert("Time = 0", () => track.CurrentTime == 0);
            AddStep("Seek(33)", () => composer.SeekTo(33));
            AddAssert("Time = 33", () => track.CurrentTime == 33);
            AddStep("Seek(89)", () => composer.SeekTo(89));
            AddAssert("Time = 89", () => track.CurrentTime == 89);

            // Backwards
            AddStep("Seek(25)", () => composer.SeekTo(25));
            AddAssert("Time = 25", () => track.CurrentTime == 25);
            AddStep("Seek(0)", () => composer.SeekTo(0));
            AddAssert("Time = 0", () => track.CurrentTime == 0);
        }

        /// <summary>
        /// Tests whether seeking to exact beat times puts us on the beat time.
        /// These are the white/yellow ticks on the graph.
        /// </summary>
        private void testSeekSnappingOnBeat()
        {
            reset();

            AddStep("Seek(0), Snap", () => composer.SeekTo(0, true));
            AddAssert("Time = 0", () => track.CurrentTime == 0);
            AddStep("Seek(50), Snap", () => composer.SeekTo(50, true));
            AddAssert("Time = 50", () => track.CurrentTime == 50);
            AddStep("Seek(100), Snap", () => composer.SeekTo(100, true));
            AddAssert("Time = 100", () => track.CurrentTime == 100);
            AddStep("Seek(175), Snap", () => composer.SeekTo(175, true));
            AddAssert("Time = 175", () => track.CurrentTime == 175);
            AddStep("Seek(350), Snap", () => composer.SeekTo(350, true));
            AddAssert("Time = 350", () => track.CurrentTime == 350);
            AddStep("Seek(400), Snap", () => composer.SeekTo(400, true));
            AddAssert("Time = 400", () => track.CurrentTime == 400);
            AddStep("Seek(450), Snap", () => composer.SeekTo(450, true));
            AddAssert("Time = 450", () => track.CurrentTime == 450);
        }

        /// <summary>
        /// Tests whether seeking to somewhere in the middle between beats puts us on the expected beats.
        /// For example, snapping between a white/yellow beat should put us on either the yellow or white, depending on which one we're closer too.
        /// If
        /// </summary>
        private void testSeekSnappingInBetweenBeat()
        {
            reset();

            AddStep("Seek(24), Snap", () => composer.SeekTo(24, true));
            AddAssert("Time = 0", () => track.CurrentTime == 0);
            AddStep("Seek(26), Snap", () => composer.SeekTo(26, true));
            AddAssert("Time = 50", () => track.CurrentTime == 50);
            AddStep("Seek(150), Snap", () => composer.SeekTo(150, true));
            AddAssert("Time = 100", () => track.CurrentTime == 100);
            AddStep("Seek(170), Snap", () => composer.SeekTo(170, true));
            AddAssert("Time = 175", () => track.CurrentTime == 175);
            AddStep("Seek(274), Snap", () => composer.SeekTo(274, true));
            AddAssert("Time = 175", () => track.CurrentTime == 175);
            AddStep("Seek(276), Snap", () => composer.SeekTo(276, true));
            AddAssert("Time = 350", () => track.CurrentTime == 350);
        }

        /// <summary>
        /// Tests that when seeking forward with no beat snapping, beats are never snapped to, nor the next timing point (if we've skipped it).
        /// </summary>
        private void testSeekForwardNoSnapping()
        {
            reset();

            AddStep("SeekForward", () => composer.SeekForward());
            AddAssert("Time = 50", () => track.CurrentTime == 50);
            AddStep("SeekForward", () => composer.SeekForward());
            AddAssert("Time = 100", () => track.CurrentTime == 100);
            AddStep("SeekForward", () => composer.SeekForward());
            AddAssert("Time = 200", () => track.CurrentTime == 200);
            AddStep("SeekForward", () => composer.SeekForward());
            AddAssert("Time = 400", () => track.CurrentTime == 400);
            AddStep("SeekForward", () => composer.SeekForward());
            AddAssert("Time = 450", () => track.CurrentTime == 450);
        }

        /// <summary>
        /// Tests that when seeking forward with beat snapping, all beats are snapped to and timing points are never skipped.
        /// </summary>
        private void testSeekForwardSnappingOnBeat()
        {
            reset();

            AddStep("SeekForward", () => composer.SeekForward(true));
            AddAssert("Time = 50", () => track.CurrentTime == 50);
            AddStep("SeekForward", () => composer.SeekForward(true));
            AddAssert("Time = 100", () => track.CurrentTime == 100);
            AddStep("SeekForward", () => composer.SeekForward(true));
            AddAssert("Time = 175", () => track.CurrentTime == 175);
            AddStep("SeekForward", () => composer.SeekForward(true));
            AddAssert("Time = 350", () => track.CurrentTime == 350);
            AddStep("SeekForward", () => composer.SeekForward(true));
            AddAssert("Time = 400", () => track.CurrentTime == 400);
            AddStep("SeekForward", () => composer.SeekForward(true));
            AddAssert("Time = 450", () => track.CurrentTime == 450);
        }

        /// <summary>
        /// Tests that when seeking forward from in-between two beats, the next beat or timing point is snapped to, and no beats are skipped.
        /// This will also test being extremely close to the next beat/timing point, to ensure rounding is not an issue.
        /// </summary>
        private void testSeekForwardSnappingFromInBetweenBeat()
        {
            reset();

            AddStep("Seek(49)", () => composer.SeekTo(49));
            AddStep("SeekForward", () => composer.SeekForward(true));
            AddAssert("Time = 50", () => track.CurrentTime == 50);
            AddStep("Seek(49.999)", () => composer.SeekTo(49.999));
            AddStep("SeekForward", () => composer.SeekForward(true));
            AddAssert("Time = 50", () => track.CurrentTime == 50);
            AddStep("Seek(99)", () => composer.SeekTo(99));
            AddStep("SeekForward", () => composer.SeekForward(true));
            AddAssert("Time = 100", () => track.CurrentTime == 100);
            AddStep("Seek(99.999)", () => composer.SeekTo(99.999));
            AddStep("SeekForward", () => composer.SeekForward(true));
            AddAssert("Time = 100", () => track.CurrentTime == 100);
            AddStep("Seek(174)", () => composer.SeekTo(174));
            AddStep("SeekForward", () => composer.SeekForward(true));
            AddAssert("Time = 175", () => track.CurrentTime == 175);
            AddStep("Seek(349)", () => composer.SeekTo(349));
            AddStep("SeekForward", () => composer.SeekForward(true));
            AddAssert("Time = 350", () => track.CurrentTime == 350);
            AddStep("Seek(399)", () => composer.SeekTo(399));
            AddStep("SeekForward", () => composer.SeekForward(true));
            AddAssert("Time = 400", () => track.CurrentTime == 400);
            AddStep("Seek(449)", () => composer.SeekTo(449));
            AddStep("SeekForward", () => composer.SeekForward(true));
            AddAssert("Time = 450", () => track.CurrentTime == 450);
        }

        private void reset()
        {
            AddStep("Reset", () => composer.SeekTo(0));
        }

        private class TestHitObjectComposer : HitObjectComposer
        {
            public TestHitObjectComposer(Ruleset ruleset)
                : base(ruleset)
            {
            }

            protected override IReadOnlyList<ICompositionTool> CompositionTools => new ICompositionTool[0];
        }

        private class TimingPointVisualiser : CompositeDrawable
        {
            private readonly Track track;

            private readonly Drawable tracker;

            public TimingPointVisualiser(Beatmap beatmap, Track track)
            {
                this.track = track;

                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                Width = 0.75f;

                FillFlowContainer timelineContainer;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        Name = "Background",
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black.Opacity(85f)
                    },
                    new Container
                    {
                        Name = "Tracks",
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding(15),
                        Children = new[]
                        {
                            tracker = new Box
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.Centre,
                                RelativeSizeAxes = Axes.Y,
                                RelativePositionAxes = Axes.X,
                                Width = 2,
                                Colour = Color4.Red,
                            },
                            timelineContainer = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Spacing = new Vector2(0, 5)
                            },
                        }
                    }
                };

                var timingPoints = beatmap.ControlPointInfo.TimingPoints;

                for (int i = 0; i < timingPoints.Count; i++)
                {
                    TimingControlPoint next = i == timingPoints.Count - 1 ? null : timingPoints[i + 1];
                    timelineContainer.Add(new TimingPointTimeline(timingPoints[i], next?.Time ?? beatmap.HitObjects.Last().StartTime, track.Length));
                }
            }

            protected override void Update()
            {
                base.Update();

                tracker.X = (float)(track.CurrentTime / track.Length);
            }

            private class TimingPointTimeline : CompositeDrawable
            {
                public TimingPointTimeline(TimingControlPoint timingPoint, double endTime, double fullDuration)
                {
                    RelativeSizeAxes = Axes.X;
                    AutoSizeAxes = Axes.Y;

                    Box createMainTick(double time) => new Box
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomCentre,
                        RelativePositionAxes = Axes.X,
                        X = (float)(time / fullDuration),
                        Height = 10,
                        Width = 2
                    };

                    Box createBeatTick(double time) => new Box
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomCentre,
                        RelativePositionAxes = Axes.X,
                        X = (float)(time / fullDuration),
                        Height = 5,
                        Width = 2,
                        Colour = time > endTime ? Color4.Gray : Color4.Yellow
                    };

                    AddInternal(createMainTick(timingPoint.Time));
                    AddInternal(createMainTick(endTime));

                    for (double t = timingPoint.Time + timingPoint.BeatLength / 4; t < fullDuration; t += timingPoint.BeatLength / 4)
                        AddInternal(createBeatTick(t));
                }
            }
        }
    }
}
