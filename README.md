# JSONConvertDataSet
C# JSON Convert To DataSet. 
Support multiple tables and maintain relationships

Interpretation:

{
  "ID":"001",
  "Name":"Gavin",
  "Friends":[{
      "Name":"XiaoMing",
      "Sex":"male",
      "Age":23,
      "phone":[
         "8888888888",
         "7777777777"
        ],
       "address":"Moon"
      },
      {
      "Name":"XiaoWang",
      "Sex":"male",
      "Age":28,
      "phone":[
         "5555555555",
         "6666666666"
        ],
       "address":"Moon"
      }],  
  "address":"earth" 
}

	DataSet Tables:
	Table1: _data_	
	data_uuid							ID	Name	Friends								address
	5130dfd17f92455591769ae34bb2ae8c	001	Gavin	712a2afe9d9d476086c6a4f57bda02ab	earth
	
	Table2: Friends
	data_uuid							Name		Sex		Age	Phone								address
	712a2afe9d9d476086c6a4f57bda02ab	XiaoMing	male	23	7329bd462203490d89afff933840b220	Moon
	712a2afe9d9d476086c6a4f57bda02ab	XiaoWang	male	28	b6951927426d4003af14d761ab8051a0	Moon
	
	Table3: Friends.Phone
	data_uuid							phone0		phone1
	7329bd462203490d89afff933840b220	8888888888	7777777777
	b6951927426d4003af14d761ab8051a0	5555555555	6666666666
	
	
	Friends.data_uuid=_data_.Friends
	Friends.Phone.data_uuid=Friends.Phone
      
