﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Tests.Visual;
using OpenTK;
using osu.Game.Rulesets.Osu.Mods;
using OpenTK.Graphics;
using osu.Game.Rulesets.Osu.Judgements;

namespace osu.Game.Rulesets.Osu.Tests
{
    [Ignore("getting CI working")]
    public class TestCaseHitCircle : OsuTestCase
    {
        private readonly Container content;
        protected override Container<Drawable> Content => content;

        private bool auto;
        private bool hidden;
        private int depthIndex;
        private int circleSize;
        private float circleScale;

        public TestCaseHitCircle()
        {
            base.Content.Add(content = new OsuInputManager(new RulesetInfo { ID = 0 }));

            AddStep("Single", () => addSingle());
            AddStep("Stream", addStream);
            AddToggleStep("Auto", v => auto = v);
            AddToggleStep("Hidden", v => hidden = v);
            AddSliderStep("CircleSize", 0, 10, 0, s => circleSize = s);
            AddSliderStep("CircleScale", 0.5f, 2, 1, s => circleScale = s);
        }

        private void addSingle(double timeOffset = 0, Vector2? positionOffset = null)
        {
            positionOffset = positionOffset ?? Vector2.Zero;

            var circle = new HitCircle
            {
                StartTime = Time.Current + 1000 + timeOffset,
                Position = positionOffset.Value,
                ComboColour = Color4.LightSeaGreen
            };

            circle.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty { CircleSize = circleSize });

            var drawable = new TestDrawableHitCircle(circle, auto)
            {
                Anchor = Anchor.Centre,
                Scale = new Vector2(circleScale),
                Depth = depthIndex++
            };

            if (auto)
                drawable.State.Value = ArmedState.Hit;

            if (hidden)
                drawable.ApplyCustomUpdateState += new TestOsuModHidden().CustomSequence;

            Add(drawable);
        }

        private void addStream()
        {
            Vector2 pos = Vector2.Zero;

            for (int i = 0; i <= 1000; i += 100)
            {
                addSingle(i, pos);
                pos += new Vector2(10);
            }
        }

        private class TestOsuModHidden : OsuModHidden
        {
            public new void CustomSequence(DrawableHitObject drawable, ArmedState state) => base.CustomSequence(drawable, state);
        }

        private class TestDrawableHitCircle : DrawableHitCircle
        {
            private readonly bool auto;

            public TestDrawableHitCircle(OsuHitObject h, bool auto) : base(h)
            {
                this.auto = auto;
            }

            protected override void CheckForJudgements(bool userTriggered, double timeOffset)
            {
                if (auto && !userTriggered && timeOffset > 0)
                {
                    // pretend we really hit it
                    AddJudgement(new OsuJudgement
                    {
                        Result = HitObject.ScoreResultForOffset(timeOffset)
                    });
                }
                base.CheckForJudgements(userTriggered, timeOffset);
            }
        }
    }
}
