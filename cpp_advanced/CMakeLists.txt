cmake_minimum_required(VERSION 3.14)
project(crtp_trading LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

add_executable(trading main.cpp)

# Aggressive optimization for benchmarking
if(MSVC)
    target_compile_options(trading PRIVATE /O2 /W4)
else()
    target_compile_options(trading PRIVATE -O2 -Wall -Wextra -march=native)
endif()
