using System;

namespace MI_GUI_WinUI.Models
{
    public class GenerationProgress : IEquatable<GenerationProgress>
    {
        public float Progress { get; set; }
        public GenerationProgressState State { get; set; }
        public string StateText => GetStateText();
        public bool IsIndeterminate => State == GenerationProgressState.Loading ||
                                     State == GenerationProgressState.Tokenizing ||
                                     State == GenerationProgressState.Encoding ||
                                     State == GenerationProgressState.InitializingLatents;

        private string GetStateText()
        {
            return State switch
            {
                GenerationProgressState.NotStarted => "Ready to generate",
                GenerationProgressState.Initializing => "Initializing...",
                GenerationProgressState.Loading => "Loading model...",
                GenerationProgressState.Tokenizing => "Processing text...",
                GenerationProgressState.Encoding => "Encoding...",
                GenerationProgressState.InitializingLatents => "Preparing tensors...",
                GenerationProgressState.Generating => $"Generating... {(Progress * 100f):F0}%",
                GenerationProgressState.Diffusing => $"Diffusing... {(Progress * 100f):F0}%",
                GenerationProgressState.Decoding => "Converting to image...",
                GenerationProgressState.Finalizing => "Finalizing...",
                GenerationProgressState.Completing => "Saving image...",
                GenerationProgressState.Completed => "Generation complete",
                GenerationProgressState.Failed => "Generation failed",
                GenerationProgressState.Cancelled => "Generation cancelled",
                _ => "Unknown state"
            };
        }

        public bool Equals(GenerationProgress other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Progress.Equals(other.Progress) && State == other.State;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((GenerationProgress)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Progress, State);
        }

        public static bool operator ==(GenerationProgress left, GenerationProgress right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(GenerationProgress left, GenerationProgress right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"{State} - {Progress:P0}";
        }
    }
}
