# BarySim
**BarySim** is a handcrafted, high-performance N-body physics simulation engine written in C#.  
It models gravitational systems using SIMD-accelerated particles and recursive spatial partitioning via multidimensional binary trees (Barnes-Hut).

---

## Features

- **Jerk-aware motion integration**  
  Includes drag forces, collision logic, boundary behavior, and numerically stable position updates.

- **Barnes-Hut approximation**  
  Efficient spatial acceleration using dynamic, hyperdimensional binary trees.

- **Real-time multithreading**  
  Thread lifecycle coordination via manual synchronization (AutoResetEvent / CountdownEvent).

- **Console-based renderer**  
  Terminal rendering with an FPS overlay: percentile-based performance graph with delay and wall-time diagnostics.

- **Zero-GC simulation core**  
  No per-frame heap allocations — data is reused, stack-local, and memory-coherent.

- **High particle count support**  
  Recursive merging, impulse computation, and drag response included in a deterministic simulation pipeline.

- **Designed for extensibility**  
  Uses CRTP, modular simulation layers, and abstract particle/tree strategies for injection of alternate physics or spatial models.


Press F1 to toggle the control overlay. Ctrl+Fx defaults parameters.

![2022-01-09 01_06_38-Greenshot](https://user-images.githubusercontent.com/3358169/162883807-195b8cd3-d48b-4f67-ba3f-61aa4e8b6720.jpg)
![2022-01-23 16_45_06-Baryon Simulator 3D - 24296 Particles (15 FPS)](https://user-images.githubusercontent.com/3358169/162883558-610d2347-ec80-4e28-92de-7ead73df8a1a.png)
![2022-01-21 11_55_01-Baryon Simulator 3D - 38666 Particles (7 5 FPS)](https://user-images.githubusercontent.com/3358169/162883597-cdef63aa-d5c8-4bb5-8a6a-95395cb95d6c.png)
![2022-01-18 01_00_58-Baryon Simulator 3D - 6807 Particles (30 FPS)](https://user-images.githubusercontent.com/3358169/162883654-86f4c539-4268-40ac-a639-0231386c7b00.png)
![2022-01-12 23_53_26-Greenshot](https://user-images.githubusercontent.com/3358169/162883736-c237a59d-ac02-48e0-96d4-27abe60e8c9a.jpg)
![2022-01-08 21_32_27-Greenshot](https://user-images.githubusercontent.com/3358169/162883941-5eb977cc-e006-45ca-b3ac-204a2abf0264.jpg)
