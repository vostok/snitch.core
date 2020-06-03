using Vostok.Hosting.Abstractions;

namespace Vostok.Snitch.Core.Models
{
    internal class ApplicationIdentity : IVostokApplicationIdentity
    {
        public string Project { get; }
        public string Subproject { get; }
        public string Environment { get; }
        public string Application { get; }
        public string Instance { get; }

        public ApplicationIdentity(string project, string subproject, string environment, string application, string instance)
        {
            Project = project;
            Subproject = subproject;
            Environment = environment;
            Application = application;
            Instance = instance;
        }

        protected bool Equals(ApplicationIdentity other) =>
            Project == other.Project && Subproject == other.Subproject && Environment == other.Environment && Application == other.Application && Instance == other.Instance;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((ApplicationIdentity)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Project.GetHashCode();
                hashCode = (hashCode * 397) ^ (Subproject != null ? Subproject.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Environment.GetHashCode();
                hashCode = (hashCode * 397) ^ Application.GetHashCode();
                hashCode = (hashCode * 397) ^ Instance.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString() =>
            $"{nameof(Project)}: {Project}, {nameof(Subproject)}: {Subproject}, {nameof(Environment)}: {Environment}, {nameof(Application)}: {Application}, {nameof(Instance)}: {Instance}";
    }
}