OSCdef.new(
	\playNote,
	{arg msg;
		var args = msg[1].asString.split($ );
		var freq = args[0].asFloat;
		var amp = args[1].asFloat;
		var dur = args[2].asFloat;
		var part = args[3].asSymbol;
		part.postln;
		if(part === '\\oboe1',
			{(
				{amp*
					(0.01*Saw.ar(1320,5,0)*SinOsc.kr(55,0.1,0.1,0) +
						SinOsc.ar(freq,SinOsc.kr(3.456),amp)
					)
					*EnvGen.kr(
						Env([1,1,1,1,1,0], [dur/4,dur/4,dur/4,dur/4]
				), doneAction: Done.freeSelf); }.play;
			)
		},{});
		if(part.asSymbol === '\\clar2',{
			(
				{amp*
					(0.01*Saw.ar(800,5,0)*SinOsc.kr(55,SinOsc.kr(0.55),0.1,0) +
						SinOsc.ar(freq,SinOsc.kr(1.456),0.9,0-amp/10+0.2)
					)
					*EnvGen.kr(
						Env([1,1,1,1,0], [dur/4,dur/4,dur/4,dur/4]
				), doneAction: Done.freeSelf); }.play;
			)
		},{})
	},
	'/notePlayer',
	nil ,57120
);