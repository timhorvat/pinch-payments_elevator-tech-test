using System.Data;

namespace pinch_payments_tech_test
{
    public class Elevator
    {
        private int _currentFloor;

        private readonly int _travelTime;
        private readonly int _pendingTime;

        private List<int> _upFloors;
        private List<int> _downFloors;

        private Status _currentStatus;
        private Direction _currentDirection;

        // Building limits
        private readonly int minFloor = 0;
        private readonly int maxFloor = 10;

        public int CurrentFloor => _currentFloor;
        public Direction CurrentDirection => _currentDirection;

        public bool HasPendingRequests() => _upFloors.Count > 0 || _downFloors.Count > 0;

        public Elevator()
        {
            Console.WriteLine("Initialising Elevator.");
            _currentFloor = 0;

            _travelTime = 1;
            _pendingTime = 3;

            _upFloors = [];
            _downFloors = [];

            _currentStatus = Status.IDLE;
            _currentDirection = Direction.IDLE;
        }

        private Task SimulateElevatorMovement()
        {
            return Task.Delay(TimeSpan.FromSeconds(_travelTime));
        }

        private Task SimulateElevatorPending()
        {
            return Task.Delay(TimeSpan.FromSeconds(_pendingTime));
        }

        private bool ShouldStopHere()
        {
            if (_currentDirection == Direction.UP)
            {
                return _upFloors.Contains(_currentFloor);
            }
            if (_currentDirection == Direction.DOWN)
            {
                return _downFloors.Contains(_currentFloor);
            }

            // If idle, stop only if any set includes current floor
            return _upFloors.Contains(_currentFloor) || _downFloors.Contains(_currentFloor);
        }

        private async Task ServiceStopHere()
        {
            // Open, service both directions at this floor, close
            Console.WriteLine($"Open doors at {_currentFloor}");

            if (_currentDirection == Direction.UP)
            {
                _upFloors.Remove(_currentFloor);
            }
            else if (_currentDirection == Direction.DOWN)
            {
                _downFloors.Remove(_currentFloor);
            }
            else
            {
                _upFloors.Remove(_currentFloor);
                _downFloors.Remove(_currentFloor);
            }

            await SimulateElevatorPending();

            Console.WriteLine($"Close doors at {_currentFloor}");

            // If nothing pending, set idle
            if (!HasPendingRequests())
            {
                _currentDirection = Direction.IDLE;
            }
        }

        private bool HasAheadInDirection()
        {
            if (_currentDirection == Direction.UP)
            {
                return _upFloors.Any((floor) => floor >= _currentFloor);
            }
            if (_currentDirection == Direction.DOWN)
            {
                return _downFloors.Any((floor) => floor <= _currentFloor);
            }
            return false;
        }

        private bool HasOppositeDirectionPending()
        {
            if (_currentDirection == Direction.UP)
            {
                return _downFloors.Count > 0;
            }
            if (_currentDirection == Direction.DOWN)
            {
                return _upFloors.Count > 0;
            }
            return false;
        }

        // TODO: refactor
        private int? NearestPending()
        {
            var all = _upFloors.Concat(_downFloors).ToList();
            if (all.Count == 0) return null;
            int nearest = all.OrderBy(f => Math.Abs(f - _currentFloor)).ThenBy(f => f).First();
            return nearest;
        }

        public async Task Step()
        {
            if (!HasPendingRequests())
            {
                _currentDirection = Direction.IDLE;
            }

            // If idle, pick up direction based on nearest pending stop
            if (_currentDirection == Direction.IDLE)
            {
                // find the closest floor - maybe the next requested floor?
                int? nearest = NearestPending();
                if (nearest.HasValue)
                {
                    _currentDirection = nearest.Value > _currentFloor
                        ? Direction.UP
                        : nearest.Value < _currentFloor
                            ? Direction.DOWN
                            : Direction.IDLE;
                }
            }

            if (ShouldStopHere())
            {
                await ServiceStopHere();
                return;
            }

            // If no further stops ahead in current direction, consider reversing
            if (!HasAheadInDirection())
            {
                // If there are stops in the opposite direction, reverse
                if (HasOppositeDirectionPending())
                {
                    _currentDirection = _currentDirection == Direction.UP
                        ? Direction.DOWN
                        : Direction.UP;

                    if (ShouldStopHere())
                    {
                        await ServiceStopHere();
                        return;
                    }
                }
                else
                {
                    _currentDirection = Direction.IDLE;
                    return;
                }
            }

            await SimulateElevatorMovement();

            int nextFloor = _currentFloor + (_currentDirection == Direction.UP ? 1 : -1);
            nextFloor = Math.Min(Math.Max(nextFloor, minFloor), maxFloor);
            if (nextFloor == _currentFloor)
            {
                _currentDirection = _currentDirection == Direction.UP ? Direction.DOWN : Direction.UP;
                return;
            }

            Console.WriteLine($"Move {((_currentDirection == Direction.UP) ? "up" : "down")} to {nextFloor}");
            _currentFloor = nextFloor;

            if (ShouldStopHere())
            {
                await ServiceStopHere();
            }
        }


        private async void MoveElevator()
        {
            if (_currentStatus != Status.IDLE)
            {
                return;
            }
            _currentStatus = Status.MOVING;

            Console.WriteLine("Moving elevator.");

            while(HasPendingRequests())
            {
                await Step();
            };

            _currentStatus = Status.IDLE;
        }

        // Someone calling the elevator from OUTSIDE the elevator
        public void HallCall(int floor, Direction direction)
        {
            // validate floor

            if (direction == Direction.DOWN)
            {
                _downFloors.Add(floor);
                _downFloors = [.. _downFloors.OrderDescending()];
            }

            else if (direction == Direction.UP)
            {
                _upFloors.Add(floor);
                _upFloors = [.. _upFloors.Order()];
            }

            if (floor == _currentFloor)
            {
                _currentDirection = direction;
            }
            else
            {
                // find the closest floor - maybe the next requested floor?
                int? nearest = NearestPending();
                if (nearest.HasValue)
                {
                    _currentDirection = nearest.Value > _currentFloor
                        ? Direction.UP
                        : nearest.Value < _currentFloor
                            ? Direction.DOWN
                            : Direction.IDLE;
                }

            }

            MoveElevator();
        }

        // Someone requesting a floor INSIDE the elevator
        public void CarCall(int floor)
        {
            // validate floor

            if (floor == _currentFloor)
            {
                return;
            }


            if (floor > _currentFloor)
            {
                _upFloors.Add(floor);
                _upFloors = [.. _upFloors.Order()];
            }
            else
            {
                _downFloors.Add(floor);
                _downFloors = [.. _downFloors.OrderDescending()];
            }

            if (_currentDirection == Direction.IDLE)
            {
                _currentDirection = floor > _currentFloor
                    ? Direction.UP
                    : Direction.DOWN;
            }

            MoveElevator();
        }

        public void Reset(int startFloor = 0)
        {
            _currentFloor = startFloor;
            _currentDirection = Direction.IDLE;
            _upFloors.Clear();
            _downFloors.Clear();
            Console.WriteLine($"Reset at {_currentFloor}");
        }

    }
}
