#ifndef __INCLUDE_GUARD_COMPILER_UTILS__
#define __INCLUDE_GUARD_COMPILER_UTILS__

#define COMBINE_2_TOKENS_(a, b) a ## b
#define COMBINE_2_TOKENS(a, b) COMBINE_2_TOKENS_(a, b)

#define STRINGIFY_(a) #a
#define STRINGIFY(a) STRINGIFY_(a)

#endif