# Pinch Payments - Elevator Technical Test

## Summary

This is the Elevator Technical Test for Pinch Payments written as a C# .NET console application.


## Commands

	- hall <floor> <up|down>   - hall call at a floor
	- car <floor>              - car call to a floor
	- test <1|2|3|4>           - run a predefined scenario
	- help
	- exit


## Tests

	1. Passenger summons lift on the ground floor. Once in, choose to go to level 5.
	2. Passenger summons lift on level 6 to go down. Passenger on level 4 summons the lift to go down. They both choose L1.
	3. Passenger 1 summons lift to go up from L2. Passenger 2 summons lift to go down from L4. Passenger 1 chooses to go to L6. Passenger 2 chooses to go to Ground Floor.
	4. Passenger 1 summons lift to go up from Ground. They choose L5. Passenger 2 summons lift to go down from L4. Passenger 3 summons lift to go down from L10. Passengers 2 and 3 choose to travel to Ground.

