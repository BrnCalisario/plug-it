using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Security.Jwt;

namespace Reddit.Controllers;

using Model;
using Repositories;
using DTO;
using Services;

[ApiController]
[EnableCors("MainPolicy")]
[Route("group")]
public class GroupController : Controller
{
    [HttpPost("list")]
    public async Task<ActionResult<List<Group>>> ListGroups(
    [FromServices] IGroupRepository groupRepository,
    [FromServices] IUserService userService,
    [FromBody] Jwt jwt
)
    {
        User user;
        try
        {
            user = await userService.ValidateUserToken(jwt);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }

        if (user is null)
            return NotFound();

        var allGroups = await groupRepository.Filter(g => true);
        var userGroups = await groupRepository.GetUserGroups(user);


        // Precisando deixar o Get User Quantity Assincrono
        var result = new List<GroupDTO>();
        foreach (var g in allGroups)
        {
            result.Add(new GroupDTO
            {
                Name = g.Name,
                Description = g.Description,
                ImageId = g.Image,
                isMember = userGroups.Any(ug => g.Id == ug.Id),
                UserQuantity = await groupRepository.GetUserQuantity(g),
            });
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Post(
        [FromServices] IGroupRepository groupRepo,
        [FromServices] IUserService userService,
        [FromBody] CreateGroupDTO groupData
    )
    {
        User user;
        try
        {
            user = await userService.ValidateUserToken(new Jwt { Value = groupData.Jwt });
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }

        if (user is null)
            return NotFound();

        var duplicates = await groupRepo.Filter(g => g.Name == groupData.Name.ToLower());

        if (duplicates.Count() > 0)
            return BadRequest("Group already exists");

        Group group = new Group()
        {
            OwnerId = user.Id,
            Name = groupData.Name.ToLower(),
            Description = groupData.Description,
            CreationDate = DateTime.Now,
        };

        await groupRepo.Add(group);

        var query = await groupRepo.Filter(g => g.Name == group.Name);
        int groupId = query.First().Id;

        return Ok(groupId);
    }

    [HttpPut]
    public async Task<ActionResult> Update(
        [FromBody] GroupDTO groupData,
        [FromServices] IGroupRepository groupRepository,
        [FromServices] IUserService userService
    )
    {
        Group group = await groupRepository.Find(groupData.Id);

        if (group is null)
            return NotFound("Grupo não encontrado");

        User user;
        try
        {
            user = await userService.ValidateUserToken(new Jwt { Value = groupData.Jwt });
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }

        if (user is null)
            return NotFound();

        if (group.Description != groupData.Description && !string.IsNullOrWhiteSpace(groupData.Description))
            group.Description = groupData.Description;

        await groupRepository.Update(group);

        return Ok();
    }

    [HttpDelete]
    public async Task<ActionResult> Delete(
        [FromBody] GroupDTO groupData,
        [FromServices] IGroupRepository groupRepository,
        [FromServices] IUserService userService
    )
    {
        Group group = await groupRepository.Find(groupData.Id);

        if (group is null)
            return NotFound();

        User user;
        try
        {
            user = await userService.ValidateUserToken(new Jwt { Value = groupData.Jwt });
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }

        if (user is null)
            return NotFound();

        bool canDrop = await groupRepository.HasPermission(user, group, PermissionEnum.DropGroup);

        if (!canDrop || group.OwnerId != user.Id)
            return BadRequest();

        await groupRepository.Delete(group);

        return Ok();
    }

    [HttpPost("enter")]
    public async Task<ActionResult> AddMember(
        [FromBody] MemberDTO memberData,
        [FromServices] IGroupRepository groupRepository,
        [FromServices] IUserService userService
    )
    {
        Group group = await groupRepository.Find(memberData.GroupId);

        if (group is null)
            return BadRequest();

        User user;
        try
        {
            user = await userService.ValidateUserToken(new Jwt { Value = memberData.Jwt });
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }

        if (user is null)
            return NotFound();

        await groupRepository.AddMember(group, user);

        return Ok();
    }

    [HttpDelete("exit-group")]
    public async Task<ActionResult> ExitGroup(
        [FromBody] MemberDTO memberData,
        [FromServices] IGroupRepository groupRepository,
        [FromServices] IUserService userService
    )
    {
        Group group = await groupRepository.Find(memberData.GroupId);

        if (group is null)
            return BadRequest();

        User user;
        try
        {
            user = await userService.ValidateUserToken(new Jwt { Value = memberData.Jwt });
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }

        if (user is null)
            return NotFound();

        if (group.OwnerId == user.Id)
            return BadRequest("Owner can't quit");

        await groupRepository.RemoveMember(group, user);

        return Ok();
    }

    [HttpDelete("remove-member")]
    public async Task<ActionResult> RemoveMember(
        [FromBody] MemberDTO memberData,
        [FromServices] IGroupRepository groupRepository,
        [FromServices] IUserService userService,
        [FromServices] IUserRepository userRepository
    )
    {
        Group group = await groupRepository.Find(memberData.GroupId);

        if (group is null)
            return BadRequest();

        User user;
        try
        {
            user = await userService.ValidateUserToken(new Jwt { Value = memberData.Jwt });
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
        if (user is null)
            return NotFound();


        bool canBan = await groupRepository.HasPermission(user, group, PermissionEnum.Ban);

        if (!canBan)
            return BadRequest("Don't have permission");

        var target = await userRepository.Find(memberData.UserId);

        if (target is null)
            return NotFound("User not found");

        await groupRepository.RemoveMember(group, target);

        return Ok();
    }

    [HttpPost("promote-member")]
    public async Task<ActionResult> PromoteRole(
        [FromBody] MemberRoleDTO memberData,
        [FromServices] IGroupRepository groupRepository,
        [FromServices] IUserRepository userRepository,
        [FromServices] IUserService userService,
        [FromServices] IRepository<Role> roleRepository
    )
    {
        Group group = await groupRepository.Find(memberData.GroupId);

        if (group is null)
            return BadRequest();

        User user;
        try
        {
            user = await userService.ValidateUserToken(new Jwt { Value = memberData.Jwt });
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
        if (user is null)
            return NotFound();

        var role = await roleRepository.Find(memberData.RoleId);

        if(role is null)
            return NotFound("Role not found");

        await groupRepository.PromoteMember(group, user, role);

        return Ok();
    }

}
