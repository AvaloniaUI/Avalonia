#include "common.h"

static int Counter = 0;
AvnInsidePotentialDeadlock::AvnInsidePotentialDeadlock()
{
    Counter++;
}

AvnInsidePotentialDeadlock::~AvnInsidePotentialDeadlock()
{
    Counter--;
}

bool AvnInsidePotentialDeadlock::IsInside()
{
    return Counter!=0;
}
