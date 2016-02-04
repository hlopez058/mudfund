# mudfund

Stock Trading AI Application. Meant to track stocks and simulate performance.


1.	Factory
  a.	Fetch stock prices
  b.	Read different api’s to increase reliability
  c.	Store in such a way the ai can read back granularity but still be efficient
  d.	Possible compression 

2.	AI Predictor
  a.	Interfaces with factory class and reads data for AI processing.
  b.	Spits out the findings as behavior info. For each stock. (ie. Healthy , sick, confidence level,…etc..)

3.	AI Trading 
  a.	Interfaces with prediction stage and determines how to buy/sell stocks
  b.	List of rules for when to buy or sell based on stock behavior and user input 
    i.	stop-loss
    ii.	 adjust for fees
    iii.	 external reviews 
    iv.	Market predictions
  c.	Places trades into a “banker” class

4.	Banker 
  a.	Real 
    i.	A timely alert system to users in order to make physical sells/buy orders
    ii.	Can be an email or tied directly into market API’s
  b.	Simulator 
    i.	A buy or sell order from the AItrader is simulated.
    ii.	Takes into account fees and transaction time
    iii.	Monitors market to determine possible gains/losses on positions made
    iv.	Creates feedback for AIpredictor and is used in scoring AI .
    v.	Gives overall portfolio yield to date

5.	Watcher (ELK Stack) 
  a.	Uses Elastic Search, Logstash and Kibana
  b.	Interfaces with classes and writes data to logs.
  c.	Logs are pulled into Kibana Webserver for viewing with charts etc.  
