﻿Handling CSV files
===============

This folder contains the Active Events necessary to parse CSV data. There are two basic Active Events, that converts between CSV and lambda.

* __[p5.csv.csv2lambda]__ - Converts from CSV to lambda
* __[p5.csv.lambda2csv]__ - Converts from lambda to CSV

The **[load-file]** Active Event from p5.io, will automatically invoke the **[p5.csv.csv2lambda]** for .csv files, unless you explicitly tell it not to.
To convert a CSV file, you can use the following code.

```
load-file:~/documents/private/sample.csv
```

The above code, assumes you have a CSV file in your private documents folder.
The **[p5.csv.lambda2csv]** Active Event, will reverse the process, and creates a piece of CSV text, which you can save or do whatever you wish 
with, after invocation. Example are given below.

```
p5.csv.lambda2csv
  item1
    name:Thomas
    status:cool
  item2
    name:John
    status:male
  item3
    name:jane
    status:female
```

The above invocation will return the following.

```
p5.csv.lambda2csv:@"name,status
Thomas,cool
John,male
jane,female
"
```
