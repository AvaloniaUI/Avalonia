#version 450
	layout(location = 0) in vec3 aPos;
	layout(location = 1) in vec3 aNormal;

	layout(location = 0) out vec3 FragPos;
	layout(location = 1) out vec3 VecPos;  
	layout(location = 2) out vec3 Normal;

	layout(push_constant) uniform constants{
        float maxY;
    	float minY;
        float time;
        float disco;
    	mat4 model;
    };
    
    layout(binding = 0) uniform UniformBufferObject {
        mat4 projection;
    } ubo;
    
	void main()
	{
		float discoScale = sin(time * 10.0) / 10.0;
		float distortionX = 1.0 + disco * cos(time * 20.0) / 10.0;
		
		float scale = 1.0 + disco * discoScale;
		
		vec3 scaledPos = aPos;
		scaledPos.x = scaledPos.x * distortionX;
		
		scaledPos *= scale;
		gl_Position = ubo.projection * model * vec4(scaledPos, 1.0);
		FragPos = vec3(model * vec4(aPos, 1.0));
		VecPos = aPos;
		Normal = normalize(vec3(model * vec4(aNormal, 1.0)));
	}