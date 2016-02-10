module.exports = function(grunt) {
	
  	grunt.initConfig({

    	pkg: grunt.file.readJSON('package.json'),

        execute: {
            target: {
                src: ['app.js']
            }
        },

    	jade: {
    		compile: {
                options: {
                    client: false,
                    pretty: true,
                    data: grunt.file.readJSON("digestBitly.json")
                },
    			files: {
    				"digest.html": ["digest.jade"]
    			}
    		}
    	}
	});

    grunt.loadNpmTasks('grunt-execute');
	grunt.loadNpmTasks('grunt-contrib-jade');
	
	grunt.registerTask('default', ['execute','jade']);

};

