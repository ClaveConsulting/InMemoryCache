# InMemoryCache

Efficient and thread safe in-memory cache for dotnet.


## How to use


```csharp
var cache = new InMemoryCache();

var value = cache.TryGetOrAdd(key, timeSpan, valueFactory);
var value = await cache.TryGetorAdd(key, timeSpan, asyncValueFactory);

```

The `TryGetOrAdd` function is thread-safe and smart. There is both an async (`Task<T>`) and sync version. 

* It uses Lazy
* It uses stale-while-revalidate

## Improvements over MemoryCache

InMemoryCache is designed for high load and slow factory functions. Consider a simple scenario:

### 1. Single thread cache

```
         time +---->

         +----------------+---------+-------+                  +---------------+                      +----------------+---------+-------+
Thread 1 | CacheMiss(key) | Factory | Store |---(some time)--->| CacheHit(key) |-----(a long time)--->| CacheMiss(key) | Factory | Store |
         +----------------+---------+-------+                  +---------------+					  +----------------+---------+-------+
```

This is the simplest scenario, a single thread tries to read some value from the cache, but the cache does not contain the key so it calls the factory function to create the value and store it.
Then some time later it checks again, and this time the key is in the cache, and so it retreives it. Much later again the cached value has expired, and so it will have a second cache miss and will be forced to call the factory function again.

This is fine if you only have a single thread, but lets consider what happens if multiple threads try to access the cache for the same key at the same time


### 2. Multiple threads

```
         time +---->
		 
         +----------------+---------+-------+
Thread 1 | CacheMiss(key) | Factory | Store |
         +-+--------------+-+-------+-+-----+-+
Thread 2   | CacheMiss(key) | Factory | Store |
           +-+--------------+-+-------+-+-----+-+
Thread 3     | CacheMiss(key) | Factory | Store |
             +-+--------------+-+-------+-+-----+-+
Thread 4       | CacheMiss(key) | Factory | Store |
               +----------------+---------+-------+
```

In this scenario multiple threads are trying to access the same key almost at the same time. Each thread gets a cache miss and therefore they invoke the factory function and when it is done they store the result.
This means that the potentially slow and heavy factory function is called many times and the result of all but the last time is overwritten.

An improvement is to not store the result of the factory but to store a `Lazy` value. 

```
         time +---->
		 
         +----------------+-----------------+
Thread 1 | CacheMiss(key) |      Factory    |
         +-+--------------++----------------+
Thread 2   | CacheHit(key) |        Wait    |
           +-+-------------+-+--------------+
Thread 3     | CacheHit(key) |      Wait    |
             +-+-------------+-+------------+
Thread 4       | CacheHit(key) |    Wait    |
               +---------------+------------+
```

Now the first thread stores the lazy value before the factory is done, and the other threads will wait for the same value. The result is tha the factory function is only called once.

## 3. Stale while revalidate

The second problem with the first example above is that when the cached item expires then the poor thread is forced to wait for the factory to get its value. 
It would be better if the thread could use the existing stale value while the factory is working on getting a new fresh value.


```
                           +---------------+        +----------------+                  
Thread 1 ---(some time)--->| CacheHit(key) |------->| CacheMiss(key) |
                           +---------------+	    +----------------+-------+
Background Thread                                   |      Factory   | Store |
                                                    +----------------+-------+
```

Here the factory is called in a background thread while Thread 1 uses the previous stale value. This way it does not have to wait for the factory to finish but can continue with the previous value.
Note that this only happens for a short time, if the cached value expired a long time ago then the stale value is forgotten and cannot be used.

## License

The MIT license