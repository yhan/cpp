# bug fix: 
future news ( comparing to publish time) should not be published

# Optimization
## 1. News sorting

news sorted at arrival, publish time early break

0. My submission
    news are not sorted, sort at publish time : N*Log(N)
    
    newid < 2^32
    N = 2^32
    N * log(N) =  137,438,953,472  <<<<  bad !
    
    N + log(N) = 4,294,967,328


1. lg(n) + n' + n*lg(n') 
   at news arrival : sort by news age
   + at publish: iterate then break early + sort by 3 keys
   
 
2. lg(n) + n 
   at news arrival : sort by 3 keys 
   + publish: iterate over all 
   
when max_age limit is small: n' + n*lg(n') << n


## 2. Rate limiting:
     per sub: CountInWindow(sorted_times, pubtime-1s, pubtime)
     => you need just a COUNT: so Queue<ts> enqueue when add, 
      to have the window COUNT: COUNT(queue) - RECOUNT(dequeue to publish-1s)
      
   old impl:  (issue : MEMORY GROWS)
   get per sub DeliveryTimestamps grows, each time per news/sub decided to publish, grow the list
   then at publish time : find the COUNT between [now-1s, now]   2*Log(k)  (k is the per sub timestamps COUNT)
   
   new impl:
   Keep a queue<ts> on subscription
   each time decide to publish news on sub, enqueue, 
   at publish time : drop ts where ts older than now-1s, COUNT the queue is baseline


## 3. Publish optim: 
   i converted O(c*s) to O(c*topics * subset_subscriptions) 
   is this really a optimization ? only if S >> topics * subset_subscriptions
   if one news covers all topics then this is not an optim; which should not be the case in the real world