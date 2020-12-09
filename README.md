# IronPDF deadlock demo

1. Run `dotnet run`
2. Open http://localhost:5000/seq
3. Observe that it works
4. Open http://localhost:5000/simple
5. Observe that it's stuck in a deadlock
   - Should you not observe a deadlock then increase `Worker._iterations` and try again
6. Open http://localhost:5000/seq
7. Observe that this no longer works
8. 😭

When fixed you can run http://localhost:5000/adv to run a more advanced scenario.
