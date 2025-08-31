# ⚡ SwiftMapper

![SwiftMapper](Assets/SwiftMapper.png)

[![NuGet](https://img.shields.io/nuget/v/SwiftMapper.svg)](https://www.nuget.org/packages/SwiftMapper/1.0.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.txt)
[![Build](https://img.shields.io/github/actions/workflow/status/<your-username>/SwiftMapper/build.yml?branch=main)](https://github.com/<your-username>/SwiftMapper/actions)

SwiftMapper is a **fast, lightweight object-to-object mapper** for .NET.  
Inspired by AutoMapper, but optimized for **simplicity and speed**.

---

## ✨ Features

- 🚀 High-performance property mapping  
- 🔹 Maps by matching property names  
- 🔹 Supports **nested object mapping**  
- 🔹 Maps **lists and collections**  
- 🔹 Converts **enums** to/from underlying ints or names  
- 🔹 Allows injecting sub-objects not present in the source  

---

## 📦 Install

From your project directory:

```bash
dotnet add package SwiftMapper
```

Or via Package Manager:

```powershell
Install-Package SwiftMapper
```

---

## ⚡ Quick Start

### 1) Flat + nested mapping

```csharp
using SwiftMapper;

var user = new User
{
    Id = 42,
    Name = "Ada",
    Status = UserStatus.Active,
    Address = new Address { Street = "123 Main", City = "London" }
};

var userDto = Mapper.Map<User, UserDto>(user);
// userDto.Id == 42
// userDto.Name == "Ada"
// userDto.Status == 1           // enum -> underlying int
// userDto.Address != null       // nested object mapped by name
```

### 2) List mapping

```csharp
var order = new Order
{
    OrderId = 1001,
    Items = new List<OrderItem>
    {
        new OrderItem { Sku = "ABC", Quantity = 2 },
        new OrderItem { Sku = "XYZ", Quantity = 5 }
    }
};

var orderDto = Mapper.Map<Order, OrderDto>(order);
// orderDto.Items.Count == 2
```

### 3) Inject a sub-object

```csharp
var withProfile = new WithProfile { Id = "p-123" };
var injectedProfile = new ProfileDto { DisplayName = "Ada Lovelace", Email = "ada@example.com" };

var withProfileDto = Mapper.Map<WithProfile, WithProfileDto>(withProfile, ("Profile", injectedProfile));
// withProfileDto.Profile references injectedProfile
```

---

## 🛠 API

```csharp
TDestination Mapper.Map<TSource, TDestination>(
    TSource source,
    params (string SubPropertyName, object PropertyValue)[] subObjectValues)
    where TSource : class
    where TDestination : class, new();
```

- Maps writable destination properties by **name** from the source  
- Nested objects and lists are mapped **recursively**  
- Enums convert to underlying ints (and strings parse into enums)  
- `subObjectValues` lets you inject destination sub-objects by name  

---

## 📌 Notes

- Source must be non-null.  
- Read-only destination properties are skipped.  
- Mapping uses reflection. For **performance-sensitive** paths, consider caching or limiting excessive allocations.  

---

## 🤝 Contributing

Contributions are welcome! 🎉  
- Open an **issue** for bugs or feature requests  
- Submit **PRs** with tests for new features or fixes  

---

## 📜 License

MIT License – see [LICENSE.txt](LICENSE.txt).

---