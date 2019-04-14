﻿// adapted from https://github.com/madskristensen/ExtensibilityTools

using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace VerilogLanguage
{
    internal sealed class OutliningTagger : ITagger<IOutliningRegionTag>
    {
        readonly ITextBuffer buffer;
        ITextSnapshot snapshot;
        IEnumerable<Region> regions;
        private bool _hasBufferchanged;
        private Timer _timer;
        private bool _isParsing;

        public OutliningTagger(ITextBuffer buffer)
        {
            this.buffer = buffer;
            this.snapshot = buffer.CurrentSnapshot;
            this.regions = new List<Region>();
            this.ReParse();
            this.buffer.Changed += BufferChanged;

            // TODO: Replace this with OnIdle logic
            _timer = new Timer(1000);
            _timer.Elapsed += timer_Elapsed;
            _timer.Start();
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!_hasBufferchanged || _isParsing)
                return;

            _isParsing = true;
            _timer.Stop();

            ReParse();

            this.TagsChanged(this, new SnapshotSpanEventArgs(
                    new SnapshotSpan(this.snapshot, 0, this.snapshot.Length))
                );

            _timer.Start();
            _hasBufferchanged = false;
            _isParsing = false;
        }

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;

            IEnumerable<Region> currentRegions = this.regions;
            ITextSnapshot currentSnapshot = this.snapshot;
            SnapshotSpan entire = new SnapshotSpan(spans[0].Start, spans[spans.Count - 1].End).TranslateTo(currentSnapshot, SpanTrackingMode.EdgeExclusive);
            int startLineNumber = entire.Start.GetContainingLine().LineNumber;
            int endLineNumber = entire.End.GetContainingLine().LineNumber;

            foreach (var region in currentRegions)
            {
                if (region.StartLine <= endLineNumber && region.EndLine >= startLineNumber)
                {
                    var startLine = currentSnapshot.GetLineFromLineNumber(region.StartLine);
                    string text = startLine.GetText();
                    string hover = entire.Snapshot.GetText(region.StartOffset, region.EndOffset - region.StartOffset);

                    yield return new TagSpan<IOutliningRegionTag>(
                        new SnapshotSpan(currentSnapshot, region.StartOffset, region.EndOffset - region.StartOffset),
                        new OutliningRegionTag(false, true, text, hover));
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            if (e.After != buffer.CurrentSnapshot || _isParsing)
                return;

            _hasBufferchanged = true;
        }

        void ReParse()
        {
            ITextSnapshot newSnapshot = buffer.CurrentSnapshot;
            List<Region> newRegions = new List<Region>();
            Region currentRegion = null;
            ITextSnapshotLine prev = null;

            foreach (var line in newSnapshot.Lines)
            {
                string text = line.GetText();

                if (!string.IsNullOrWhiteSpace(text) && text[0] == '[' && currentRegion == null)
                {
                    currentRegion = new Region
                    {
                        StartLine = line.LineNumber,
                        StartOffset = line.Start.Position
                    };
                }
                else if (currentRegion != null)
                {
                    if (line.LineNumber == newSnapshot.LineCount - 1 && !string.IsNullOrWhiteSpace(text))
                    {
                        currentRegion.EndLine = line.LineNumber;
                        currentRegion.EndOffset = line.End.Position;
                        newRegions.Add(currentRegion);
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(text) || text[0] == '[')
                    {
                        currentRegion.EndLine = prev.LineNumber;
                        currentRegion.EndOffset = prev.End.Position;
                        newRegions.Add(currentRegion);

                        currentRegion = null;
                    }

                    if (!string.IsNullOrWhiteSpace(text) && text[0] == '[')
                    {
                        currentRegion = new Region
                        {
                            StartLine = line.LineNumber,
                            StartOffset = line.Start.Position
                        };
                    }
                }

                prev = line;
            }

            this.snapshot = newSnapshot;
            this.regions = newRegions.Where(line => line.StartLine != line.EndLine);
        }
    }

    class Region
    {
        public int StartLine { get; set; }
        public int StartOffset { get; set; }
        public int EndLine { get; set; }
        public int EndOffset { get; set; }
    }
}
