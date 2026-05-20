namespace Project.Staff.Jobs
{
    public sealed class StaffIdleJobBehaviour : IStaffJobBehaviour
    {
        private readonly StaffJobContext context;

        public StaffIdleJobBehaviour(StaffJobContext context)
        {
            this.context = context;
        }

        public void Tick()
        {
            if (context == null || context.Movement == null)
            {
                return;
            }

            UnityEngine.Vector3 idlePosition = context.GetIdlePosition();

            if (context.Movement.HasReachedPosition(idlePosition))
            {
                context.Movement.Stop();
                context.Movement.FaceForwardFlat();
                return;
            }

            context.Movement.SetDestination(idlePosition);
        }

        public void Reset()
        {
        }
    }
}