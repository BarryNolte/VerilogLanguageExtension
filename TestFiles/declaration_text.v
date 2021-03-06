// sample from https://stackoverflow.com/questions/2341824/syntax-for-using-an-array-of-wires-as-input

// 8-bit wire ports:
module wow();
 output wire [7:0] r,
 input  wire [7:0] x,
 input  wire [7:0] y

// 1-bit wide by 8-bit deep wire arrays 
 wire rbit    [7:0];
 wire onebit  [7:0];
 wire twocomp [7:0];

// wire declarations
wire [7:0] rbit;
wire [7:0] onebit;
wire [7:0] twocomp; 
endmodule
